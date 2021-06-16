using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Expressions.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.ExpressionTranslators.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal.Mapping;
using Npgsql.EntityFrameworkCore.PostgreSQL.Utilities;
using static Npgsql.EntityFrameworkCore.PostgreSQL.Utilities.Statics;

namespace Npgsql.EntityFrameworkCore.PostgreSQL.Query.Internal
{
    public class NpgsqlSqlTranslatingExpressionVisitor : RelationalSqlTranslatingExpressionVisitor
    {
        static readonly ConstructorInfo DateTimeCtor1 =
            typeof(DateTime).GetConstructor(new[] { typeof(int), typeof(int), typeof(int) });

        static readonly ConstructorInfo DateTimeCtor2 =
            typeof(DateTime).GetConstructor(new[] { typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int) });

        static readonly MethodInfo Like2MethodInfo =
            typeof(DbFunctionsExtensions)
                .GetRuntimeMethod(nameof(DbFunctionsExtensions.Like), new[] { typeof(DbFunctions), typeof(string), typeof(string) });

        // ReSharper disable once InconsistentNaming
        static readonly MethodInfo ILike2MethodInfo
            = typeof(NpgsqlDbFunctionsExtensions)
                .GetRuntimeMethod(nameof(NpgsqlDbFunctionsExtensions.ILike), new[] { typeof(DbFunctions), typeof(string), typeof(string) });

        static readonly MethodInfo EnumerableAnyWithPredicate
            = typeof(Enumerable).GetTypeInfo().GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Single(mi => mi.Name == nameof(Enumerable.Any) && mi.GetParameters().Length == 2);

        static readonly MethodInfo EnumerableAll
            = typeof(Enumerable).GetTypeInfo().GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Single(mi => mi.Name == nameof(Enumerable.All) && mi.GetParameters().Length == 2);

        static readonly MethodInfo EnumerableContains
            = typeof(Enumerable).GetTypeInfo().GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Single(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2);

        static readonly MethodInfo ObjectEquals
            = typeof(object).GetRuntimeMethod(nameof(object.Equals), new[] { typeof(object), typeof(object) });

        readonly NpgsqlSqlExpressionFactory _sqlExpressionFactory;
        readonly IRelationalTypeMappingSource _typeMappingSource;
        readonly NpgsqlJsonPocoTranslator _jsonPocoTranslator;

        static Type _nodaTimePeriodType;

        public NpgsqlSqlTranslatingExpressionVisitor(
            [NotNull] RelationalSqlTranslatingExpressionVisitorDependencies dependencies,
            [NotNull] QueryCompilationContext queryCompilationContext,
            [NotNull] QueryableMethodTranslatingExpressionVisitor queryableMethodTranslatingExpressionVisitor)
            : base(dependencies, queryCompilationContext, queryableMethodTranslatingExpressionVisitor)
        {
            _sqlExpressionFactory = (NpgsqlSqlExpressionFactory)dependencies.SqlExpressionFactory;
            _jsonPocoTranslator = ((NpgsqlMemberTranslatorProvider)Dependencies.MemberTranslatorProvider).JsonPocoTranslator;
            _typeMappingSource = dependencies.TypeMappingSource;
        }

        // PostgreSQL COUNT() always returns bigint, so we need to downcast to int
        public override SqlExpression TranslateCount(SqlExpression sqlExpression)
        {
            Check.NotNull(sqlExpression, nameof(sqlExpression));

            return _sqlExpressionFactory.Convert(
                _sqlExpressionFactory.ApplyDefaultTypeMapping(
                    _sqlExpressionFactory.Function(
                        "COUNT",
                        new[] { sqlExpression },
                        nullable: false,
                        argumentsPropagateNullability: FalseArrays[1],
                        typeof(long))),
                typeof(int), _typeMappingSource.FindMapping(typeof(int)));
        }

        // In PostgreSQL SUM() doesn't return the same type as its argument for smallint, int and bigint.
        // Cast to get the same type.
        // http://www.postgresql.org/docs/current/static/functions-aggregate.html
        public override SqlExpression TranslateSum(SqlExpression sqlExpression)
        {
            Check.NotNull(sqlExpression, nameof(sqlExpression));

            var inputType = sqlExpression.Type.UnwrapNullableType();

            // Note that there is no Sum over short in LINQ
            if (inputType == typeof(int))
                return _sqlExpressionFactory.Convert(
                    _sqlExpressionFactory.Function(
                        "SUM",
                        new[] { sqlExpression },
                        nullable: true,
                        argumentsPropagateNullability: FalseArrays[1],
                        typeof(long)),
                    inputType,
                    sqlExpression.TypeMapping);

            if (inputType == typeof(long))
                return _sqlExpressionFactory.Convert(
                    _sqlExpressionFactory.Function(
                        "SUM",
                        new[] { sqlExpression },
                        nullable: true,
                        argumentsPropagateNullability: FalseArrays[1],
                        typeof(decimal)),
                    inputType,
                    sqlExpression.TypeMapping);

            return _sqlExpressionFactory.Function(
                "SUM",
                new[] { sqlExpression },
                nullable: true,
                argumentsPropagateNullability: FalseArrays[1],
                inputType,
                sqlExpression.TypeMapping);
        }

        /// <summary>
        ///     Translates Aggregate data as array using PostgreSQL function ARRAY_AGG.
        /// </summary>
        /// <param name="fieldExpression"> An expression to translate Aggregate over. </param>
        /// <returns> A SQL translation of String aggregate over the given expression. </returns>
        public virtual SqlExpression TranslateArrayAgg([NotNull] SqlExpression fieldExpression)
        {
            Check.NotNull(fieldExpression, nameof(fieldExpression));

            return (SqlExpression)_sqlExpressionFactory.Function(
                "ARRAY_AGG",
                new[] { fieldExpression },
                nullable: true,
                argumentsPropagateNullability: new[] { false },
                fieldExpression.Type.MakeArrayType());
        }

        /// <summary>
        ///     Translates Aggregate bitwise "AND" using PostgreSQL function BIT_AND .
        /// </summary>
        /// <param name="fieldExpression"> An expression to translate Aggregate over. </param>
        /// <returns> A SQL translation of String aggregate over the given expression. </returns>
        public virtual SqlExpression TranslateBitAnd([NotNull] SqlExpression fieldExpression)
        {
            Check.NotNull(fieldExpression, nameof(fieldExpression));

            return (SqlExpression)_sqlExpressionFactory.Function(
                "BIT_AND",
                new[] { fieldExpression },
                nullable: true,
                argumentsPropagateNullability: new[] { false },
                fieldExpression.Type);
        }

        /// <summary>
        ///     Translates Aggregate bitwise "OR" using PostgreSQL function BIT_OR .
        /// </summary>
        /// <param name="fieldExpression"> An expression to translate Aggregate over. </param>
        /// <returns> A SQL translation of String aggregate over the given expression. </returns>
        public virtual SqlExpression TranslateBitOr([NotNull] SqlExpression fieldExpression)
        {
            Check.NotNull(fieldExpression, nameof(fieldExpression));

            return (SqlExpression)_sqlExpressionFactory.Function(
                "BIT_OR",
                new[] { fieldExpression },
                nullable: true,
                argumentsPropagateNullability: new[] { false },
                fieldExpression.Type);
        }

        /// <summary>
        ///     Translates Aggregate if all are true using PostgreSQL function BOOL_AND .
        /// </summary>
        /// <param name="fieldExpression"> An expression to translate Aggregate over. </param>
        /// <returns> A SQL translation of String aggregate over the given expression. </returns>
        public virtual SqlExpression TranslateBoolAnd([NotNull] SqlExpression fieldExpression)
        {
            Check.NotNull(fieldExpression, nameof(fieldExpression));

            return (SqlExpression)_sqlExpressionFactory.Function(
                "BOOL_AND",
                new[] { fieldExpression },
                nullable: true,
                argumentsPropagateNullability: new[] { false },
                fieldExpression.Type);
        }

        /// <summary>
        ///     Translates Aggregate if at least is true using PostgreSQL function BOOL_OR .
        /// </summary>
        /// <param name="fieldExpression"> An expression to translate Aggregate over. </param>
        /// <returns> A SQL translation of String aggregate over the given expression. </returns>
        public virtual SqlExpression TranslateBoolOr([NotNull] SqlExpression fieldExpression)
        {
            Check.NotNull(fieldExpression, nameof(fieldExpression));

            return (SqlExpression)_sqlExpressionFactory.Function(
                "BOOL_OR",
                new[] { fieldExpression },
                nullable: true,
                argumentsPropagateNullability: new[] { false },
                fieldExpression.Type);
        }

        /// <summary>
        ///     Translates Aggregate strings using PostgreSQL function STRING_AGG.
        /// </summary>
        /// <param name="fieldExpression"> An expression to translate Aggregate over. </param>
        /// <param name="delimiterExpression"> An expression to delimitate between string. </param>
        /// <returns> A SQL translation of String aggregate over the given expression. </returns>
        public virtual SqlExpression TranslateStringAgg([NotNull] SqlExpression fieldExpression, [NotNull] SqlExpression delimiterExpression)
        {
            Check.NotNull(fieldExpression, nameof(fieldExpression));
            Check.NotNull(delimiterExpression, nameof(delimiterExpression));

            return (SqlExpression)_sqlExpressionFactory.Function(
                "STRING_AGG",
                new[] { fieldExpression, delimiterExpression },
                nullable: true,
                argumentsPropagateNullability: new[] { false, false },
                fieldExpression.Type,
                fieldExpression.TypeMapping);
        }

        /// <summary>
        ///     Translates Aggregate data as array using PostgreSQL function ARRAY_AGG.
        /// </summary>
        /// <param name="fieldExpression"> An expression to translate Aggregate over. </param>
        /// <returns> A SQL translation of String aggregate over the given expression. </returns>
        public virtual SqlExpression TranslateJsonAgg([NotNull] SqlExpression fieldExpression)
        {
            Check.NotNull(fieldExpression, nameof(fieldExpression));

            return (SqlExpression)_sqlExpressionFactory.Function(
                "JSON_AGG",
                new[] { fieldExpression },
                nullable: true,
                argumentsPropagateNullability: new[] { false },
                //typeof(object[]));
                fieldExpression.Type.MakeArrayType());
        }

        /// <summary>
        ///     Translates Aggregate data as array using PostgreSQL function ARRAY_AGG.
        /// </summary>
        /// <param name="fieldExpression"> An expression to translate Aggregate over. </param>
        /// <returns> A SQL translation of String aggregate over the given expression. </returns>
        public virtual SqlExpression TranslateJsonObjectAgg([NotNull] SqlExpression keyExpression, [NotNull] SqlExpression valueExpression)
        {
            Check.NotNull(keyExpression, nameof(keyExpression));
            Check.NotNull(valueExpression, nameof(valueExpression));

            return (SqlExpression)_sqlExpressionFactory.Function(
                "JSON_OBJECT_AGG",
                new[] { keyExpression, valueExpression },
                nullable: true,
                argumentsPropagateNullability: new[] { false },
                typeof(string));
                //typeof(Dictionary<,>).MakeGenericType(keyExpression.Type, valueExpression.Type),
                //_typeMappingSource.FindMapping(
                //    typeof(System.Text.Json.JsonDocument)
                //));
        }


        /// <inheritdoc />
        protected override Expression VisitUnary(UnaryExpression unaryExpression)
        {
            if (unaryExpression.NodeType == ExpressionType.ArrayLength)
            {
                if (TranslationFailed(unaryExpression.Operand, Visit(unaryExpression.Operand), out var sqlOperand))
                    return null;

                // Translate Length on byte[], but only if the type mapping is for bytea. There's also array of bytes
                // (mapped to smallint[]), which is handled below with CARDINALITY.
                if (sqlOperand.Type == typeof(byte[]) &&
                    (sqlOperand.TypeMapping == null || sqlOperand.TypeMapping is NpgsqlByteArrayTypeMapping))
                {
                    return _sqlExpressionFactory.Function(
                        "length",
                        new[] { sqlOperand },
                        nullable: true,
                        argumentsPropagateNullability: TrueArrays[1],
                        typeof(int));
                }

                return _jsonPocoTranslator.TranslateArrayLength(sqlOperand) ??
                       _sqlExpressionFactory.Function(
                           "cardinality",
                           new[] { sqlOperand },
                           nullable: true,
                           argumentsPropagateNullability: TrueArrays[1],
                           typeof(int));
            }

            return base.VisitUnary(unaryExpression);
        }

        protected override Expression VisitMethodCall(MethodCallExpression methodCall)
        {
            var visited = base.VisitMethodCall(methodCall);
            if (visited != null)
                return visited;

            if (methodCall.Arguments.Count > 0 && (
                    methodCall.Arguments[0].Type.IsArray || methodCall.Arguments[0].Type.IsGenericList()))
            {
                return VisitArrayMethodCall(methodCall.Method, methodCall.Arguments);
            }

            // GroupBy Aggregate case
            if (methodCall.Object == null
                && methodCall.Method.DeclaringType == typeof(NpgsqlIEnumerableExtensions)
                && methodCall.Arguments.Count > 0)
            {
                var groupingElementExpression = GroupingElementExpression.DirtyCast(Visit(methodCall.Arguments[0]));
                if (groupingElementExpression != null) 
                {
                    switch (methodCall.Method.Name)
                    {
                        case nameof(NpgsqlIEnumerableExtensions.ArrayAgg):
                            if (methodCall.Arguments.Count == 2)
                            {
                                groupingElementExpression = groupingElementExpression.ApplySelector(
                                    Visit(RemapLambda(groupingElementExpression, UnwrapLambdaFromQuote(methodCall.Arguments[1])))
                                );
                            }
                            return TranslateArrayAgg(
                                GetExpressionForAggregation(groupingElementExpression));

                        case nameof(NpgsqlIEnumerableExtensions.BitAnd):
                            if (methodCall.Arguments.Count == 2)
                            {
                                groupingElementExpression = groupingElementExpression.ApplySelector(
                                    Visit(RemapLambda(groupingElementExpression, UnwrapLambdaFromQuote(methodCall.Arguments[1])))
                                );
                            }
                            return TranslateBitAnd(
                                GetExpressionForAggregation(groupingElementExpression));

                        case nameof(NpgsqlIEnumerableExtensions.BitOr):
                            if (methodCall.Arguments.Count == 2)
                            {
                                groupingElementExpression = groupingElementExpression.ApplySelector(
                                    Visit(RemapLambda(groupingElementExpression, UnwrapLambdaFromQuote(methodCall.Arguments[1])))
                                );
                            }
                            return TranslateBitOr(
                                GetExpressionForAggregation(groupingElementExpression));

                        case nameof(NpgsqlIEnumerableExtensions.BoolAnd):
                            if (methodCall.Arguments.Count == 2)
                            {
                                groupingElementExpression = groupingElementExpression.ApplySelector(
                                    Visit(RemapLambda(groupingElementExpression, UnwrapLambdaFromQuote(methodCall.Arguments[1])))
                                );
                            }
                            return TranslateBoolAnd(
                                GetExpressionForAggregation(groupingElementExpression));

                        case nameof(NpgsqlIEnumerableExtensions.BoolOr):
                            if (methodCall.Arguments.Count == 2)
                            {
                                groupingElementExpression = groupingElementExpression.ApplySelector(
                                    Visit(RemapLambda(groupingElementExpression, UnwrapLambdaFromQuote(methodCall.Arguments[1])))
                                );
                            }
                            return TranslateBoolOr(
                                GetExpressionForAggregation(groupingElementExpression));

                        case nameof(NpgsqlIEnumerableExtensions.JsonAgg):
                            if (methodCall.Arguments.Count == 2)
                            {
                                groupingElementExpression = groupingElementExpression.ApplySelector(
                                    Visit(RemapLambda(groupingElementExpression, UnwrapLambdaFromQuote(methodCall.Arguments[1])))
                                );
                            }
                            return TranslateJsonAgg(
                                GetExpressionForAggregation(groupingElementExpression));

                        case nameof(NpgsqlIEnumerableExtensions.JsonObjectAgg):
                            if (methodCall.Arguments.Count == 3)
                            {
                                var keyExpression = groupingElementExpression.ApplySelector(
                                    Visit(RemapLambda(groupingElementExpression, UnwrapLambdaFromQuote(methodCall.Arguments[1])))
                                );
                                groupingElementExpression = GroupingElementExpression.DirtyCast(Visit(methodCall.Arguments[0]));

                                var valueExpression = groupingElementExpression.ApplySelector(
                                    Visit(RemapLambda(groupingElementExpression, UnwrapLambdaFromQuote(methodCall.Arguments[2])))
                                );
                                return TranslateJsonObjectAgg(
                                    GetExpressionForAggregation(keyExpression),
                                    GetExpressionForAggregation(valueExpression)
                                    );
                            }
                            return null;

                        case nameof(NpgsqlIEnumerableExtensions.StringAgg):
                            if (methodCall.Arguments.Count == 3)
                            {
                                groupingElementExpression = groupingElementExpression.ApplySelector(
                                    Visit(RemapLambda(groupingElementExpression, UnwrapLambdaFromQuote(methodCall.Arguments[2])))
                                );
                            }
                            return TranslateStringAgg(
                                GetExpressionForAggregation(groupingElementExpression),
                                Visit(methodCall.Arguments[1]) as SqlExpression);

                    }
                }
            }

            return null;

            LambdaExpression UnwrapLambdaFromQuote(Expression expression)
                => (LambdaExpression)(expression is UnaryExpression unary && expression.NodeType == ExpressionType.Quote ? unary.Operand : expression);
            Expression RemapLambda(GroupingElementExpression groupingElement, LambdaExpression lambdaExpression)
                => ReplacingExpressionVisitor.Replace(lambdaExpression.Parameters[0], groupingElement.Element, lambdaExpression.Body);
            SqlExpression GetExpressionForAggregation(GroupingElementExpression groupingElement, bool starProjection = false)
            {
                //var selector = TranslateInternal(groupingElement.Element);
                var selector = Visit(groupingElement.Element) as SqlExpression;
                if (selector == null)
                {
                    if (starProjection)
                    {
                        selector = _sqlExpressionFactory.Fragment("*");
                    }
                    else
                    {
                        return null;
                    }
                }

                if (groupingElement.Predicate != null)
                {
                    if (selector is SqlFragmentExpression)
                    {
                        selector = _sqlExpressionFactory.Constant(1);
                    }

                    selector = _sqlExpressionFactory.Case(
                        new List<CaseWhenClause> { new(groupingElement.Predicate, selector) },
                        elseResult: null);
                }

                if (groupingElement.IsDistinct
                    && !(selector is SqlFragmentExpression))
                {
                    selector = new DistinctExpression(selector);
                }

                return selector;
            }
        }

        // Dirty Wrapper for private class Microsoft.EntityFrameworkCore.Query.RelationalSqlTranslatingExpressionVisitor+GroupingElementExpression
        sealed class GroupingElementExpression : Expression
        {
            public Expression Source { get; }
            private GroupingElementExpression(Expression group)
            {
                Source = group;
            }
            public static GroupingElementExpression DirtyCast(Expression group)
            {
                return group?.GetType()?.FullName == typeof(RelationalSqlTranslatingExpressionVisitor).FullName + "+GroupingElementExpression"
                    ? new GroupingElementExpression(group) : null;
            }

            public Expression Element { get => (Expression)Source.GetType().GetProperty(nameof(Element)).GetValue(Source); } 
            public bool IsDistinct { get => (bool)Source.GetType().GetProperty(nameof(IsDistinct)).GetValue(Source); }
            public SqlExpression Predicate { get => (SqlExpression)Source.GetType().GetProperty(nameof(Predicate)).GetValue(Source); } 
            public GroupingElementExpression ApplyDistinct() => DirtyCast((Expression)Source.GetType().GetMethod(nameof(ApplyDistinct)).Invoke(Source,Array.Empty<object>()));
            public GroupingElementExpression ApplySelector(Expression expression) => DirtyCast((Expression)Source.GetType().GetMethod(nameof(ApplySelector)).Invoke(Source, new object[] { expression }));
            public GroupingElementExpression ApplyPredicate(SqlExpression expression) => DirtyCast((Expression)Source.GetType().GetMethod(nameof(ApplyPredicate)).Invoke(Source, new[] { expression }));

        }

        /// <summary>
        /// Identifies complex array-related constructs which cannot be translated in regular method translators, since
        /// they require accessing lambdas.
        /// </summary>
        protected virtual Expression VisitArrayMethodCall(
            [NotNull] MethodInfo method, [NotNull] ReadOnlyCollection<Expression> arguments)
        {
            var array = arguments[0];

            {
                // Pattern match for: new[] { "a", "b", "c" }.Any(p => EF.Functions.Like(e.SomeText, p)),
                // which we translate to WHERE s.""SomeText"" LIKE ANY (ARRAY['a','b','c']) (see test Any_like)
                // Note: NavigationExpander normalized Any(x) to Where(x).Any()
                if (method.IsClosedFormOf(EnumerableAnyWithPredicate) &&
                    arguments[1] is LambdaExpression wherePredicate &&
                    wherePredicate.Body is MethodCallExpression wherePredicateMethodCall && (
                        wherePredicateMethodCall.Method == Like2MethodInfo ||
                        wherePredicateMethodCall.Method == ILike2MethodInfo) &&
                    wherePredicateMethodCall.Arguments is var whereArguments &&
                    whereArguments[2] == wherePredicate.Parameters[0])
                {
                    return _sqlExpressionFactory.Any(
                        (SqlExpression)Visit(whereArguments[1]),
                        (SqlExpression)Visit(array),
                        wherePredicateMethodCall.Method == Like2MethodInfo
                            ? PostgresAnyOperatorType.Like : PostgresAnyOperatorType.ILike);
                }

                // Note: we also handle the above with equality instead of Like, see NpgsqlArrayMethodTranslator
            }

            {
                // Same for All (but without the normalization)
                if (method.IsClosedFormOf(EnumerableAll) &&
                    arguments[1] is LambdaExpression wherePredicate &&
                    wherePredicate.Body is MethodCallExpression wherePredicateMethodCall && (
                        wherePredicateMethodCall.Method == Like2MethodInfo ||
                        wherePredicateMethodCall.Method == ILike2MethodInfo) &&
                    wherePredicateMethodCall.Arguments is var whereArguments &&
                    whereArguments[2] == wherePredicate.Parameters[0])
                {
                    return _sqlExpressionFactory.All(
                        (SqlExpression)Visit(wherePredicateMethodCall.Arguments[1]),
                        (SqlExpression)Visit(arguments[0]),
                        wherePredicateMethodCall.Method == Like2MethodInfo
                            ? PostgresAllOperatorType.Like : PostgresAllOperatorType.ILike);
                }
            }

            {
                if (method.IsClosedFormOf(EnumerableAnyWithPredicate) &&
                    arguments[1] is LambdaExpression wherePredicate &&
                    wherePredicate.Body is MethodCallExpression wherePredicateMethodCall)
                {
                    var predicateMethod = wherePredicateMethodCall.Method;

                    // Pattern match for: new[] { 4, 5 }.Any(p => e.SomeArray.Contains(p)),
                    // using array overlap (&&).
                    if (predicateMethod.IsClosedFormOf(EnumerableContains) &&
                        wherePredicateMethodCall.Arguments[0].Type.IsArrayOrGenericList() &&
                        wherePredicateMethodCall.Arguments[1] is ParameterExpression parameterExpression1 &&
                        parameterExpression1 == wherePredicate.Parameters[0])
                    {
                        return _sqlExpressionFactory.Overlaps(
                            (SqlExpression)Visit(arguments[0]),
                            (SqlExpression)Visit(wherePredicateMethodCall.Arguments[0]));
                    }

                    // As above, but for Contains on List<T>
                    if (predicateMethod.DeclaringType?.IsGenericType == true &&
                        predicateMethod.DeclaringType.GetGenericTypeDefinition() == typeof(List<>) &&
                        predicateMethod.Name == nameof(List<int>.Contains) &&
                        predicateMethod.GetParameters().Length == 1 &&
                        wherePredicateMethodCall.Arguments[0] is ParameterExpression parameterExpression2 &&
                        parameterExpression2 == wherePredicate.Parameters[0])
                    {
                        return _sqlExpressionFactory.Overlaps(
                            (SqlExpression)Visit(arguments[0]),
                            (SqlExpression)Visit(wherePredicateMethodCall.Object));
                    }
                }
            }

            {
                // Pattern match for: new[] { 4, 5 }.All(p => e.SomeArray.Contains(p)),
                // using array containment (<@)
                if (method.IsClosedFormOf(EnumerableAll) &&
                    arguments[1] is LambdaExpression wherePredicate &&
                    wherePredicate.Body is MethodCallExpression wherePredicateMethodCall &&
                    wherePredicateMethodCall.Method.IsClosedFormOf(EnumerableContains) &&
                    wherePredicateMethodCall.Arguments[0].Type.IsArrayOrGenericList() &&
                    wherePredicateMethodCall.Arguments[1] is ParameterExpression parameterExpression &&
                    parameterExpression == wherePredicate.Parameters[0])
                {
                    return _sqlExpressionFactory.ContainedBy(
                        (SqlExpression)Visit(arguments[0]),
                        (SqlExpression)Visit(wherePredicateMethodCall.Arguments[0]));
                }
            }

            {
                // Pattern match for: array.Any(e => e == x) (and other equality patterns)
                // Transform this to Contains.
                if (method.IsClosedFormOf(EnumerableAnyWithPredicate) &&
                    arguments[1] is LambdaExpression wherePredicate &&
                    TryMatchEquality(wherePredicate.Body, out var left, out var right) &&
                    (left == wherePredicate.Parameters[0] || right == wherePredicate.Parameters[0]))
                {
                    var item = left == wherePredicate.Parameters[0]
                        ? right
                        : right == wherePredicate.Parameters[0]
                            ? left
                            : null;

                    return item is null
                        ? null
                        : Visit(Expression.Call(EnumerableContains.MakeGenericMethod(method.GetGenericArguments()[0]), array, item));
                }

                static bool TryMatchEquality(Expression expression, out Expression left, out Expression right)
                {
                    if (expression is BinaryExpression binary)
                    {
                        (left, right) = (binary.Left, binary.Right);
                        return true;
                    }
                    if (expression is MethodCallExpression methodCall)
                    {
                        if (methodCall.Method == ObjectEquals)
                        {
                            (left, right) = (methodCall.Arguments[0], methodCall.Arguments[1]);
                            return true;
                        }

                        if (methodCall.Method.Name == nameof(object.Equals) && methodCall.Arguments.Count == 1)
                        {
                            (left, right) = (methodCall.Object, methodCall.Arguments[0]);
                            return true;
                        }
                    }

                    (left, right) = (null, null);
                    return false;
                }
            }

            return null;
        }

        /// <inheritdoc />
        protected override Expression VisitBinary(BinaryExpression binaryExpression)
        {
            if (binaryExpression.NodeType == ExpressionType.Subtract)
            {
                if (binaryExpression.Left.Type.UnwrapNullableType().FullName == "NodaTime.LocalDate" &&
                    binaryExpression.Right.Type.UnwrapNullableType().FullName == "NodaTime.LocalDate")
                {
                    if (TranslationFailed(binaryExpression.Left, Visit(TryRemoveImplicitConvert(binaryExpression.Left)), out var sqlLeft)
                        || TranslationFailed(binaryExpression.Right, Visit(TryRemoveImplicitConvert(binaryExpression.Right)), out var sqlRight))
                    {
                        return null;
                    }

                    var subtraction = _sqlExpressionFactory.MakeBinary(
                        ExpressionType.Subtract, sqlLeft, sqlRight, _typeMappingSource.FindMapping(typeof(int)));

                    return PostgresFunctionExpression.CreateWithNamedArguments(
                        "MAKE_INTERVAL",
                        new[] {  subtraction },
                        new[] { "days" },
                        nullable: true,
                        argumentsPropagateNullability: TrueArrays[1],
                        builtIn: true,
                        _nodaTimePeriodType ??= binaryExpression.Left.Type.Assembly.GetType("NodaTime.Period"),
                        typeMapping: null);
                }

                // Note: many other date/time arithmetic operators are fully supported as-is by PostgreSQL - see NpgsqlSqlExpressionFactory
            }

            if (binaryExpression.NodeType == ExpressionType.ArrayIndex)
            {
                if (TranslationFailed(binaryExpression.Left, Visit(TryRemoveImplicitConvert(binaryExpression.Left)), out var sqlLeft)
                    || TranslationFailed(binaryExpression.Right, Visit(TryRemoveImplicitConvert(binaryExpression.Right)), out var sqlRight))
                {
                    return null;
                }

                // ArrayIndex over bytea is special, we have to use function rather than subscript
                if (binaryExpression.Left.Type == typeof(byte[]))
                {
                    return _sqlExpressionFactory.Function(
                        "get_byte",
                        new[]
                        {
                            _sqlExpressionFactory.ApplyDefaultTypeMapping(sqlLeft),
                            _sqlExpressionFactory.ApplyDefaultTypeMapping(sqlRight)
                        },
                        nullable: true,
                        argumentsPropagateNullability: TrueArrays[2],
                        typeof(byte),
                        _typeMappingSource.FindMapping(typeof(byte))
                    );
                }

                return
                    // Try translating ArrayIndex inside json column
                    _jsonPocoTranslator.TranslateMemberAccess(sqlLeft, sqlRight, binaryExpression.Type) ??
                    // Other types should be subscriptable - but PostgreSQL arrays are 1-based, so adjust the index.
                    _sqlExpressionFactory.ArrayIndex(sqlLeft, GenerateOneBasedIndexExpression(sqlRight));
            }

            return base.VisitBinary(binaryExpression);
        }


        /// <inheritdoc />
        protected override Expression VisitNew(NewExpression newExpression)
        {
            if (base.VisitNew(newExpression) is { } result)
                return result;

            if (newExpression.Constructor == DateTimeCtor1)
            {
                return TryTranslateArguments(newExpression.Arguments, out var sqlArguments)
                    ? _sqlExpressionFactory.Function(
                        "make_date", sqlArguments, nullable: true, TrueArrays[3], typeof(DateTime))
                    : null;
            }

            if (newExpression.Constructor == DateTimeCtor2)
            {
                if (!TryTranslateArguments(newExpression.Arguments, out var sqlArguments))
                    return null;

                // DateTime's second component is an int, but PostgreSQL's MAKE_TIMESTAMP accepts a double precision
                sqlArguments[5] = _sqlExpressionFactory.Convert(sqlArguments[5], typeof(double));

                return _sqlExpressionFactory.Function(
                    "make_timestamp", sqlArguments, nullable: true, TrueArrays[6], typeof(DateTime));
            }

            return null;

            bool TryTranslateArguments(ReadOnlyCollection<Expression> arguments, out SqlExpression[] sqlArguments)
            {
                sqlArguments = new SqlExpression[newExpression.Arguments.Count];
                for (var i = 0; i < sqlArguments.Length; i++)
                {
                    var argument = newExpression.Arguments[i];
                    if (TranslationFailed(argument, Visit(argument), out var sqlArgument))
                    {
                        return false;
                    }

                    sqlArguments[i] = sqlArgument;
                }

                return true;
            }
        }

        /// <summary>
        /// PostgreSQL array indexing is 1-based. If the index happens to be a constant,
        /// just increment it. Otherwise, append a +1 in the SQL.
        /// </summary>
        SqlExpression GenerateOneBasedIndexExpression([NotNull] SqlExpression expression)
            => expression is SqlConstantExpression constant
                ? _sqlExpressionFactory.Constant(Convert.ToInt32(constant.Value) + 1, constant.TypeMapping)
                : (SqlExpression)_sqlExpressionFactory.Add(expression, _sqlExpressionFactory.Constant(1));

        #region Copied from RelationalSqlTranslatingExpressionVisitor

        static Expression TryRemoveImplicitConvert(Expression expression)
        {
            if (expression is UnaryExpression unaryExpression)
            {
                if (unaryExpression.NodeType == ExpressionType.Convert
                    || unaryExpression.NodeType == ExpressionType.ConvertChecked)
                {
                    var innerType = unaryExpression.Operand.Type.UnwrapNullableType();
                    if (innerType.IsEnum)
                    {
                        innerType = Enum.GetUnderlyingType(innerType);
                    }
                    var convertedType = unaryExpression.Type.UnwrapNullableType();

                    if (innerType == convertedType
                        || (convertedType == typeof(int)
                            && (innerType == typeof(byte)
                                || innerType == typeof(sbyte)
                                || innerType == typeof(char)
                                || innerType == typeof(short)
                                || innerType == typeof(ushort))))
                    {
                        return TryRemoveImplicitConvert(unaryExpression.Operand);
                    }
                }
            }

            return expression;
        }


        [DebuggerStepThrough]
        bool TranslationFailed(Expression original, Expression translation, out SqlExpression castTranslation)
        {
            if (original != null && !(translation is SqlExpression))
            {
                castTranslation = null;
                return true;
            }

            castTranslation = translation as SqlExpression;
            return false;
        }

        #endregion Copied from RelationalSqlTranslatingExpressionVisitor
    }
}
