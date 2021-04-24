/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/
using System;
using System.Runtime.Serialization;
using System.Runtime.CompilerServices;

namespace Azos
{
  /// <summary>
  /// Call guard exceptions thrown by the framework to indicate violations of value constraints.
  /// Guards are typically applied to method parameters in a fluent manner
  /// </summary>
  [Serializable]
  public class CallGuardException : AzosException
  {
    public const string DETAILS_FLD_NAME = "GUARD-DETAILS";
    public const string SITE_FLD_NAME = "GUARD-SITE";
    public const string PARAM_FLD_NAME = "GUARD-PARAM";

    public CallGuardException(string callSite, string parameterName, string message) : base(message)
    {
      CallSite = callSite;
      ParamName = parameterName;
    }

    public CallGuardException(string callSite, string parameterName, string message, Exception inner) : base(message, inner)
    {
      CallSite = callSite;
      ParamName = parameterName;
    }

    protected CallGuardException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
      PutDetailsInHttpStatus = info.GetBoolean(DETAILS_FLD_NAME);
      CallSite = info.GetString(SITE_FLD_NAME);
      ParamName = info.GetString(PARAM_FLD_NAME);
    }

    /// <summary>When set to true, will provide details in HttpStatusDescription</summary>
    public bool PutDetailsInHttpStatus { get; set; }

    /// <summary>Name of member/caller which employs the guard</summary>
    public string CallSite { get; set; }

    /// <summary>Name of parameter such as method parameter</summary>
    public string ParamName { get; set; }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
      if (info == null)
        throw new ArgumentNullException(nameof(info), GetType().Name + ".GetObjectData(info=null)");

      info.AddValue(DETAILS_FLD_NAME, PutDetailsInHttpStatus);
      info.AddValue(SITE_FLD_NAME, CallSite);
      info.AddValue(PARAM_FLD_NAME, ParamName);
      base.GetObjectData(info, context);
    }

    /// <summary>
    /// Surrounds an action by protected scope: any exception thrown by this action gets wrapped in a CallGuardException.
    /// If action is unassigned, nothing is done
    /// </summary>
    /// <remarks>
    /// You can use another version of this function with action argument in order not to create unneeded closures
    /// </remarks>
    public static void Protect(Action action,
                              [CallerFilePath]   string callFile = null,
                              [CallerLineNumber] int callLine = 0,
                              [CallerMemberName] string callMember = null)
    {
      if (action==null) return;

      try
      {
        action();
      }
      catch(Exception error)
      {
        var callSite = GuardUtils.callSiteOf(callFile, callLine, callMember);
        throw new CallGuardException(callSite,
                                     nameof(action),
                                     StringConsts.GUARDED_ACTION_SCOPE_ERROR
                                                 .Args(callSite ?? CoreConsts.UNKNOWN, error.ToMessageWithType()),
                                     error);
      }
    }

    /// <summary>
    /// Surrounds an action by protected scope: any exception thrown by this action gets wrapped in a CallGuardException.
    /// If action is unassigned, nothing is done
    /// </summary>
    /// <remarks>
    /// Use this function with action argument not to create unneeded closures
    /// </remarks>
    public static void Protect<TArg>(TArg arg, Action<TArg> action,
                              [CallerFilePath]   string callFile = null,
                              [CallerLineNumber] int callLine = 0,
                              [CallerMemberName] string callMember = null)
    {
      if (action == null) return;

      try
      {
        action(arg);
      }
      catch (Exception error)
      {
        var callSite = GuardUtils.callSiteOf(callFile, callLine, callMember);
        throw new CallGuardException(callSite,
                                     nameof(action),
                                     StringConsts.GUARDED_ACTION_SCOPE_ERROR
                                                 .Args(callSite ?? CoreConsts.UNKNOWN, error.ToMessageWithType()),
                                     error);
      }
    }
  }


  /// <summary>
  /// Provides guard utility functions such as the ones used for param value guard checking
  /// </summary>
  public static class GuardUtils
  {
    internal static string callSiteOf(string file, int line, string member)
    => "{2}@{0}:{1}".Args(file.IsNotNullOrWhiteSpace() ? System.IO.Path.GetFileName(file) : CoreConsts.UNKNOWN, line, member);

    /// <summary>
    /// Ensures that a value is not null
    /// </summary>
    public static T NonNull<T>(this T obj,
                               string name = null,
                               [CallerFilePath]   string callFile = null,
                               [CallerLineNumber] int    callLine = 0,
                               [CallerMemberName] string callMember = null) where T : class
    {
      if (obj == null)
      {
        var callSite = callSiteOf(callFile, callLine, callMember);
        throw new CallGuardException(callSite,
                                 name,
                                 StringConsts.GUARDED_CLAUSE_MAY_NOT_BE_NULL_ERROR
                                             .Args(callSite ?? CoreConsts.UNKNOWN, name ?? CoreConsts.UNKNOWN));
      }
      return obj;
    }


    /// <summary>
    /// Ensures that a nullable value-typed value is not null
    /// </summary>
    public static Nullable<T> NonNull<T>(this Nullable<T> value,
                               string name = null,
                               [CallerFilePath]   string callFile = null,
                               [CallerLineNumber] int callLine = 0,
                               [CallerMemberName] string callMember = null) where T : struct
    {
      if (!value.HasValue)
      {
        var callSite = callSiteOf(callFile, callLine, callMember);
        throw new CallGuardException(callSite,
                                 name,
                                 StringConsts.GUARDED_CLAUSE_MAY_NOT_BE_NULL_ERROR
                                             .Args(callSite ?? CoreConsts.UNKNOWN, name ?? CoreConsts.UNKNOWN));
      }
      return value;
    }

    /// <summary>
    /// Ensures that a type value is not null and is of the specified type or one of its subtypes
    /// </summary>
    public static Type IsOfType<T>(this Type type,
                               string name = null,
                               [CallerFilePath]   string callFile = null,
                               [CallerLineNumber] int callLine = 0,
                               [CallerMemberName] string callMember = null)
    {
      if (type == null || !typeof(T).IsAssignableFrom(type))
      {
        var callSite = callSiteOf(callFile, callLine, callMember);
        throw new CallGuardException(callSite,
                                 name,
                                 StringConsts.GUARDED_CLAUSE_OFTYPE_ERROR
                                             .Args(callSite ?? CoreConsts.UNKNOWN, name ?? CoreConsts.UNKNOWN, typeof(T).Name));
      }
      return type;
    }

    /// <summary>
    /// Ensures that a type value is not null and is of the specified type or one of its subtypes
    /// </summary>
    public static Type IsOfType(this Type type,
                               Type expectedType,
                               string name = null,
                               [CallerFilePath]   string callFile = null,
                               [CallerLineNumber] int callLine = 0,
                               [CallerMemberName] string callMember = null)
    {
      if (type == null || !expectedType.NonNull(nameof(expectedType)).IsAssignableFrom(type))
      {
        var callSite = callSiteOf(callFile, callLine, callMember);
        throw new CallGuardException(callSite,
                                 name,
                                 StringConsts.GUARDED_CLAUSE_OFTYPE_ERROR
                                             .Args(callSite ?? CoreConsts.UNKNOWN, name ?? CoreConsts.UNKNOWN, expectedType.Name));
      }
      return type;
    }


    /// <summary>
    /// Ensures that a value is not null and is of the specified type or one of its subtypes
    /// </summary>
    public static TValue ValueIsOfType<T, TValue>(this TValue value,
                               string name = null,
                               [CallerFilePath]   string callFile = null,
                               [CallerLineNumber] int callLine = 0,
                               [CallerMemberName] string callMember = null)
    {
      if (value == null || !typeof(T).IsAssignableFrom(value.GetType()))
      {
        var callSite = callSiteOf(callFile, callLine, callMember);
        throw new CallGuardException(callSite,
                                 name,
                                 StringConsts.GUARDED_CLAUSE_VALUEOFTYPE_ERROR
                                             .Args(callSite ?? CoreConsts.UNKNOWN, name ?? CoreConsts.UNKNOWN, typeof(T).Name));
      }
      return value;
    }

    /// <summary>
    /// Ensures that a value is not null and is of the specified type or one of its subtypes
    /// </summary>
    public static TValue ValueIsOfType<TValue>(this TValue value,
                               Type expectedType,
                               string name = null,
                               [CallerFilePath]   string callFile = null,
                               [CallerLineNumber] int callLine = 0,
                               [CallerMemberName] string callMember = null)
    {
      if (value == null || !expectedType.NonNull(nameof(expectedType)).IsAssignableFrom(value.GetType()))
      {
        var callSite = callSiteOf(callFile, callLine, callMember);
        throw new CallGuardException(callSite,
                                 name,
                                 StringConsts.GUARDED_CLAUSE_VALUEOFTYPE_ERROR
                                             .Args(callSite ?? CoreConsts.UNKNOWN, name ?? CoreConsts.UNKNOWN, expectedType.Name));
      }
      return value;
    }

    /// <summary>
    /// Ensures that the value is not null and can be type-casted to TResult, then performs type cast
    /// throwing CallGuardException otherwise
    /// </summary>
    public static TResult CastTo<TResult>(this object value,
                               string name = null,
                               [CallerFilePath]   string callFile = null,
                               [CallerLineNumber] int callLine = 0,
                               [CallerMemberName] string callMember = null)
    {
      if (value is TResult result) return result;

      var callSite = callSiteOf(callFile, callLine, callMember);
      throw new CallGuardException(callSite,
                                  name,
                                  StringConsts.GUARDED_CLAUSE_TYPECAST_ERROR
                                              .Args(callSite ?? CoreConsts.UNKNOWN, name ?? CoreConsts.UNKNOWN, typeof(TResult).DisplayNameWithExpandedGenericArgs()));
    }

    /// <summary>
    /// Ensures that a string value is non-null/blank/whitespace
    /// </summary>
    public static string NonBlank(this string str,
                                  string name = null,
                                  [CallerFilePath]   string callFile = null,
                                  [CallerLineNumber] int callLine = 0,
                                  [CallerMemberName] string callMember = null)
    {
      if (str.IsNullOrWhiteSpace())
      {
        var callSite = callSiteOf(callFile, callLine, callMember);
        throw new CallGuardException(callSite,
                                 name,
                                 StringConsts.GUARDED_STRING_CLAUSE_MAY_NOT_BE_BLANK_ERROR
                                             .Args(callSite ?? CoreConsts.UNKNOWN, name ?? CoreConsts.UNKNOWN));
      }
      return str;
    }

    /// <summary>
    /// Ensures that a string value is non-null/blank having at most the specified length
    /// </summary>
    public static string NonBlankMax(this string str,
                                     int maxLen,
                                     string name = null,
                                     [CallerFilePath]   string callFile = null,
                                     [CallerLineNumber] int callLine = 0,
                                     [CallerMemberName] string callMember = null)
    {
      var len = str.NonBlank(name, callFile, callLine, callMember).Length;
      if (len > maxLen)
      {
        var callSite = callSiteOf(callFile, callLine, callMember);
        throw new CallGuardException(callSite,
                                 name,
                                 StringConsts.GUARDED_STRING_CLAUSE_MAY_NOT_EXCEED_MAX_LEN_ERROR
                                            .Args(callSite ?? CoreConsts.UNKNOWN,
                                                  name ?? CoreConsts.UNKNOWN,
                                                  str.TakeFirstChars(15, ".."),
                                                  len,
                                                  maxLen));
      }
      return str;
    }

    /// <summary>
    /// Ensures that a string value is non-null/blank having at least the specified length
    /// </summary>
    public static string NonBlankMin(this string str,
                                     int minLen,
                                     string name = null,
                                     [CallerFilePath]   string callFile = null,
                                     [CallerLineNumber] int callLine = 0,
                                     [CallerMemberName] string callMember = null)
    {
      var len = str.NonBlank(name, callFile, callLine, callMember).Length;
      if (len < minLen)
      {
        var callSite = callSiteOf(callFile, callLine, callMember);
        throw new CallGuardException(callSite,
                                     name,
                                     StringConsts.GUARDED_STRING_CLAUSE_MAY_NOT_BE_LESS_MIN_LEN_ERROR
                                                 .Args(callSite ?? CoreConsts.UNKNOWN,
                                                  name ?? CoreConsts.UNKNOWN,
                                                  str.TakeFirstChars(15, ".."),
                                                  len,
                                                  minLen));
      }
      return str;
    }

    /// <summary>
    /// Ensures that a string value is non-null/blank having its length between the specified min/max bounds
    /// </summary>
    public static string NonBlankMinMax(this string str,
                                        int minLen,
                                        int maxLen,
                                        string name = null,
                                        [CallerFilePath]   string callFile = null,
                                        [CallerLineNumber] int callLine = 0,
                                        [CallerMemberName] string callMember = null)
    {
      var len = str.NonBlank(name, callFile, callLine, callMember).Length;
      if (len < minLen || len > maxLen)
      {
        var callSite = callSiteOf(callFile, callLine, callMember);
        throw new CallGuardException(callSite,
                                     name,
                                     StringConsts.GUARDED_STRING_CLAUSE_MUST_BE_BETWEEN_MIN_MAX_LEN_ERROR
                                                 .Args(callSite ?? CoreConsts.UNKNOWN,
                                                  name ?? CoreConsts.UNKNOWN,
                                                  str.TakeFirstChars(15, ".."),
                                                  len,
                                                  minLen,
                                                  maxLen));
      }
      return str;
    }


    /// <summary>
    /// Ensures that the condition is true
    /// </summary>
    public static T IsTrue<T>(this T value,
                               Func<T, bool>  f,
                               string name = null,
                               [CallerFilePath]   string callFile = null,
                               [CallerLineNumber] int callLine = 0,
                               [CallerMemberName] string callMember = null)
    {
      if (f!=null && !f(value))
      {
        var callSite = callSiteOf(callFile, callLine, callMember);
        throw new CallGuardException(callSite,
                                 name,
                                 StringConsts.GUARDED_CLAUSE_CONDITION_ERROR
                                             .Args(callSite ?? CoreConsts.UNKNOWN, name ?? CoreConsts.UNKNOWN));
      }
      return value;
    }

    /// <summary>
    /// Ensures that the condition is true
    /// </summary>
    public static void IsTrue(this bool condition,
                               string name = null,
                               [CallerFilePath]   string callFile = null,
                               [CallerLineNumber] int callLine = 0,
                               [CallerMemberName] string callMember = null)
    {
      if (!condition)
      {
        var callSite = callSiteOf(callFile, callLine, callMember);
        throw new CallGuardException(callSite,
                                 name,
                                 StringConsts.GUARDED_CLAUSE_CONDITION_ERROR
                                             .Args(callSite ?? CoreConsts.UNKNOWN, name ?? CoreConsts.UNKNOWN));
      }
    }


  }
}
