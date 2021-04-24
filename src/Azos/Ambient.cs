/*<FILE_LICENSE>
 * Azos (A to Z Application Operating System) Framework
 * The A to Z Foundation (a.k.a. Azist) licenses this file to you under the MIT license.
 * See the LICENSE file in the project root for more information.
</FILE_LICENSE>*/

using System;

namespace Azos
{
  /// <summary>
  /// Provides access to ambient context such as CurrentCallSession/User information.
  /// The ambient context flows through async call chains
  /// </summary>
  public static class Ambient
  {
    /// <summary>
    /// Denotes memory utilization modes
    /// </summary>
    public enum MemoryUtilizationModel
    {
      /// <summary>
      /// The application may use memory in a regular way without restraints. For example, a component
      /// may preallocate a few hundred megabyte lookup table on startup trading space for speed.
      /// </summary>
      Regular = 0,

      /// <summary>
      /// The application must try to use memory sparingly and not allocate large cache and buffers.
      /// This mode is typically used in a constrained 32bit apps and smaller servers. This mode gives
      /// a hint to components not to preallocate too much (e.g. do not preload 100 mb ZIP code database on startup)
      /// on startup. Trades performance for lower memory consumption
      /// </summary>
      Compact = -1,

      /// <summary>
      /// The application must try not to use extra memory for caches and temp buffers.
      /// This mode is typically used in a constrained 32bit apps and smaller servers.
      /// This mode is stricter than Compact
      /// </summary>
      Tiny = -2
    }


    private static MemoryUtilizationModel s_MemoryModel;


    /// <summary>
    /// Returns the memory utilization model for the application.
    /// This property is NOT configurable. It may be set at process entry point via a call to
    /// Ambient.SetMemoryModel() before the app container spawns.
    /// Typical applications should not change the defaults.
    /// Some system service providers examine this property to allocate less cache and temp buffers
    /// in the memory-constrained environments
    /// </summary>
    public static MemoryUtilizationModel MemoryModel => s_MemoryModel;



  }
}
