﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;

namespace Qactive
{
#if SERIALIZATION
  [Serializable]
#endif
  public sealed class ServiceEvaluationContext : KnownTypeContext
  {
    private static readonly HashSet<Type> extendedPrimitiveTypes = new HashSet<Type>()
      {
        typeof(CompilerGenerated),
        typeof(Guid),
        typeof(Uri),
        typeof(TimeSpan),
        typeof(DateTimeOffset),
        typeof(IQbservable<>),
        typeof(IObservable<>),
        typeof(IEnumerable<>),
        typeof(IQueryable<>)
      };

    private static readonly IEnumerable<MethodInfo> knownOperatorContainers =
              typeof(Qbservable).GetMethods(BindingFlags.Public | BindingFlags.Static)
      .Concat(typeof(Observable).GetMethods(BindingFlags.Public | BindingFlags.Static))
      .Concat(typeof(Enumerable).GetMethods(BindingFlags.Public | BindingFlags.Static))
      .Concat(typeof(Queryable).GetMethods(BindingFlags.Public | BindingFlags.Static));

    private static readonly HashSet<string> safeConcurrencyOperatorNames = new HashSet<string>()
      {
        "Delay",
        "DelaySubscription",
        "Interval",
        "Sample",
        "Throttle",
        "Timeout",
        "Timer",
      };

    private static readonly HashSet<string> safeOperatorNames = new HashSet<string>()
      {
        "Amb",
        "All",
        "And",
        "Any",
        "AsObservable",
        "AsQbservable",
        "Average",
        "Case",
        "CombineLatest",
        "Concat",
        "Contains",
        "Count",
        "DefaultIfEmpty",
        "DistinctUntilChanged",
        "ElementAt",
        "ElementAtOrDefault",
        "Empty",
        "FirstAsync",
        "FirstOrDefaultAsync",
        "ForkJoin",
        "If",
        "IgnoreElements",
        "IsEmpty",
        "LastAsync",
        "LastOrDefaultAsync",
        "Let",
        "LongCount",
        "Max",
        "MaxBy",
        "Merge",
        "Min",
        "MinBy",
        "MostRecent",
        "Never",
        "Publish",
        "PublishLast",
        "Range",
        "RefCount",
        "Return",
        "Select",
        "SelectMany",
        "SequenceEqual",
        "SingleAsync",
        "SingleOrDefaultAsync",
        "Skip",
        "SkipLast",
        "SkipUntil",
        "SkipWhile",
        "StartWith",
        "Sum",
        "Switch",
        "Take",
        "TakeLast",
        "TakeUntil",
        "TakeWhile",
        "Then",
        "TimeInterval",
        "Timestamp",
        "ToObservable",
        "ToQbservable",
        "ToQueryable",
        "Using",
        "UsingAsync",
        "When",
        "Where",
        "Zip",
      };

    private static readonly IList<MethodInfo> safeOperators = knownOperatorContainers
      .Where(method => safeOperatorNames.Contains(method.Name))
      .ToList()
      .AsReadOnly();

    private static readonly IList<MethodInfo> concurrencyOperators = knownOperatorContainers
      .Where(method => safeConcurrencyOperatorNames.Contains(method.Name))
      .ToList()
      .AsReadOnly();

    private readonly HashSet<MethodInfo> knownMethods;

    [ContractInvariantMethod]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Required for code contracts.")]
    private void ObjectInvariant()
    {
      Contract.Invariant(knownMethods != null);
    }

    public ServiceEvaluationContext(IEnumerable<MethodInfo> knownMethods, params Type[] knownTypes)
      : base(knownTypes)
    {
      Contract.Requires(knownMethods != null);
      Contract.Requires(Contract.ForAll(knownMethods, method => !method.IsGenericMethod || method.IsGenericMethodDefinition));

      this.knownMethods = new HashSet<MethodInfo>(knownMethods);
    }

    public ServiceEvaluationContext(IEnumerable<MethodInfo> knownMethods, IEnumerable<Type> knownTypes)
      : base(knownTypes)
    {
      Contract.Requires(knownMethods != null);
      Contract.Requires(Contract.ForAll(knownMethods, method => !method.IsGenericMethod || method.IsGenericMethodDefinition));

      this.knownMethods = new HashSet<MethodInfo>(knownMethods);
    }

    public ServiceEvaluationContext(IEnumerable<MethodInfo> knownMethods, IEnumerable<Assembly> knownAssemblies, params Type[] additionalKnownTypes)
      : base(knownAssemblies, additionalKnownTypes)
    {
      Contract.Requires(knownMethods != null);
      Contract.Requires(Contract.ForAll(knownMethods, method => !method.IsGenericMethod || method.IsGenericMethodDefinition));

      this.knownMethods = new HashSet<MethodInfo>(knownMethods);
    }

    public ServiceEvaluationContext(IEnumerable<MethodInfo> knownMethods, IEnumerable<Assembly> knownAssemblies, IEnumerable<Type> additionalKnownTypes)
      : base(knownAssemblies, additionalKnownTypes)
    {
      Contract.Requires(knownMethods != null);
      Contract.Requires(Contract.ForAll(knownMethods, method => !method.IsGenericMethod || method.IsGenericMethodDefinition));

      this.knownMethods = new HashSet<MethodInfo>(knownMethods);
    }

    public ServiceEvaluationContext(params Type[] knownTypes)
      : this(false, knownTypes)
    {
    }

    public ServiceEvaluationContext(IEnumerable<Type> knownTypes)
      : this(false, knownTypes)
    {
    }

    public ServiceEvaluationContext(IEnumerable<Assembly> knownAssemblies, params Type[] additionalKnownTypes)
      : this(false, knownAssemblies, additionalKnownTypes)
    {
    }

    public ServiceEvaluationContext(IEnumerable<Assembly> knownAssemblies, IEnumerable<Type> additionalKnownTypes)
      : this(false, knownAssemblies, additionalKnownTypes)
    {
    }

    public ServiceEvaluationContext(bool includeConcurrencyOperators, params Type[] knownTypes)
      : this(includeConcurrencyOperators ? safeOperators.Concat(concurrencyOperators) : safeOperators, knownTypes)
    {
    }

    public ServiceEvaluationContext(bool includeConcurrencyOperators, IEnumerable<Type> knownTypes)
      : this(includeConcurrencyOperators ? safeOperators.Concat(concurrencyOperators) : safeOperators, knownTypes)
    {
    }

    public ServiceEvaluationContext(bool includeConcurrencyOperators, IEnumerable<Assembly> knownAssemblies, params Type[] additionalKnownTypes)
      : this(includeConcurrencyOperators ? safeOperators.Concat(concurrencyOperators) : safeOperators, knownAssemblies, additionalKnownTypes)
    {
    }

    public ServiceEvaluationContext(bool includeConcurrencyOperators, IEnumerable<Assembly> knownAssemblies, IEnumerable<Type> additionalKnownTypes)
      : this(includeConcurrencyOperators ? safeOperators.Concat(concurrencyOperators) : safeOperators, knownAssemblies, additionalKnownTypes)
    {
    }

    public ServiceEvaluationContext(IEnumerable<MethodInfo> knownMethods, bool includeSafeOperators, params Type[] knownTypes)
      : this(includeSafeOperators ? knownMethods.Concat(safeOperators) : knownMethods, knownTypes)
    {
      Contract.Requires(knownMethods != null);
    }

    public ServiceEvaluationContext(IEnumerable<MethodInfo> knownMethods, bool includeSafeOperators, IEnumerable<Type> knownTypes)
      : this(includeSafeOperators ? knownMethods.Concat(safeOperators) : knownMethods, knownTypes)
    {
      Contract.Requires(knownMethods != null);
    }

    public ServiceEvaluationContext(IEnumerable<MethodInfo> knownMethods, bool includeSafeOperators, IEnumerable<Assembly> knownAssemblies, params Type[] additionalKnownTypes)
      : this(includeSafeOperators ? knownMethods.Concat(safeOperators) : knownMethods, knownAssemblies, additionalKnownTypes)
    {
      Contract.Requires(knownMethods != null);
    }

    public ServiceEvaluationContext(IEnumerable<MethodInfo> knownMethods, bool includeSafeOperators, IEnumerable<Assembly> knownAssemblies, IEnumerable<Type> additionalKnownTypes)
      : this(includeSafeOperators ? knownMethods.Concat(safeOperators) : knownMethods, knownAssemblies, additionalKnownTypes)
    {
      Contract.Requires(knownMethods != null);
    }

    public ServiceEvaluationContext(IEnumerable<MethodInfo> knownMethods, bool includeSafeOperators, bool includeConcurrencyOperators, params Type[] knownTypes)
      : this(includeConcurrencyOperators ? knownMethods.Concat(concurrencyOperators) : knownMethods, includeSafeOperators, knownTypes)
    {
      Contract.Requires(knownMethods != null);
    }

    public ServiceEvaluationContext(IEnumerable<MethodInfo> knownMethods, bool includeSafeOperators, bool includeConcurrencyOperators, IEnumerable<Type> knownTypes)
      : this(includeConcurrencyOperators ? knownMethods.Concat(concurrencyOperators) : knownMethods, includeSafeOperators, knownTypes)
    {
      Contract.Requires(knownMethods != null);
    }

    public ServiceEvaluationContext(IEnumerable<MethodInfo> knownMethods, bool includeSafeOperators, bool includeConcurrencyOperators, IEnumerable<Assembly> knownAssemblies, params Type[] additionalKnownTypes)
      : this(includeConcurrencyOperators ? knownMethods.Concat(concurrencyOperators) : knownMethods, includeSafeOperators, knownAssemblies, additionalKnownTypes)
    {
      Contract.Requires(knownMethods != null);
    }

    public ServiceEvaluationContext(IEnumerable<MethodInfo> knownMethods, bool includeSafeOperators, bool includeConcurrencyOperators, IEnumerable<Assembly> knownAssemblies, IEnumerable<Type> additionalKnownTypes)
      : this(includeConcurrencyOperators ? knownMethods.Concat(concurrencyOperators) : knownMethods, includeSafeOperators, knownAssemblies, additionalKnownTypes)
    {
      Contract.Requires(knownMethods != null);
    }

    public static bool IsExtendedPrimitiveType(Type type)
      => type != null && extendedPrimitiveTypes.Contains(type.GetIsGenericType() ? type.GetGenericTypeDefinition() : type);

    public override bool IsKnownType(Type type)
      => type != null
      && (type.GetIsEnum()
        || IsExtendedPrimitiveType(type)
        || base.IsKnownType(type));

    public bool IsKnownMethod(MethodInfo method)
    {
      if (method == null)
      {
        return false;
      }

      var type = method.DeclaringType;

      return type.GetIsEnum()
          || type.GetIsPrimitive()
          || IsExtendedPrimitiveType(type)
          || knownMethods.Contains(method.IsGenericMethod ? method.GetGenericMethodDefinition() : method);
    }

    public void AddKnownMethod(MethodInfo method)
    {
      Contract.Requires(method != null);

      knownMethods.Add(method);
    }

    public void AddKnownOperators(string name)
    {
      var matched = false;

      foreach (var method in knownOperatorContainers.Where(m => string.Equals(m.Name, name, StringComparison.Ordinal)))
      {
        matched = true;

        AddKnownMethod(method);
      }

      if (!matched)
      {
        throw new ArgumentException("Unknown method name.", "name");
      }
    }

    public void AddKnownOperators(string name, params Type[] parameterTypes)
    {
      Contract.Requires(!string.IsNullOrEmpty(name));
      Contract.Requires(parameterTypes != null);

      var matched = false;

      foreach (var method in knownOperatorContainers.Where(m => string.Equals(m.Name, name, StringComparison.Ordinal) && m.GetParameters().Select(p => p.ParameterType).SequenceEqual(parameterTypes)))
      {
        matched = true;

        AddKnownMethod(method);
      }

      if (!matched)
      {
        throw new ArgumentException("Unknown method name.", "name");
      }
    }

    internal void EnsureHasKnownOperator(string name)
    {
      Contract.Requires(!string.IsNullOrEmpty(name));

      foreach (var method in knownOperatorContainers.Where(m => string.Equals(m.Name, name, StringComparison.Ordinal)))
      {
        if (knownMethods.Contains(method))
        {
          AddKnownMethod(method);
        }
      }
    }
  }
}