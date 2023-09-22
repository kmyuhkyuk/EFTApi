﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Aki.Reflection.Utils;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;

// ReSharper disable MemberCanBePrivate.Global

namespace EFTReflection
{
    public static class RefTool
    {
        #region BindingFlags

        /// <summary>
        ///     <see cref="BindingFlags.NonPublic" /> and <see cref="BindingFlags.Instance" />
        /// </summary>
        public static readonly BindingFlags NonPublic = BindingFlags.NonPublic | BindingFlags.Instance;

        /// <summary>
        ///     <see cref="BindingFlags.Public" /> and <see cref="BindingFlags.Instance" />
        /// </summary>
        public static readonly BindingFlags Public = BindingFlags.Public | BindingFlags.Instance;

        /// <summary>
        ///     <see cref="BindingFlags.DeclaredOnly" /> and <see cref="BindingFlags.Static" />
        /// </summary>
        public static readonly BindingFlags DeclaredStatic = BindingFlags.DeclaredOnly | BindingFlags.Static;

        #endregion

        /// <summary>
        ///     Find Single <see cref="Type" /> by Lambda
        /// </summary>
        /// <param name="typePredicate"></param>
        /// <returns></returns>
        public static Type GetEftType(Func<Type, bool> typePredicate)
        {
            return PatchConstants.EftTypes.Single(typePredicate);
        }

        /// <summary>
        ///     Try Find Single <see cref="Type" /> by Lambda
        /// </summary>
        /// <param name="typePredicate"></param>
        /// <param name="eftType"></param>
        /// <returns></returns>
        public static bool TryGetEftType(Func<Type, bool> typePredicate, out Type eftType)
        {
            eftType = PatchConstants.EftTypes.SingleOrDefault(typePredicate);

            return eftType != null;
        }

        #region GetMethod

        /// <summary>
        ///     Find Single <see cref="MethodInfo" /> by Lambda
        /// </summary>
        /// <param name="type"></param>
        /// <param name="flags"></param>
        /// <param name="methodPredicate"></param>
        /// <returns></returns>
        public static MethodInfo GetEftMethod(Type type, BindingFlags flags, Func<MethodInfo, bool> methodPredicate)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return type.GetMethods(flags).Single(methodPredicate);
        }

        /// <summary>
        ///     Find Single <see cref="MethodInfo" /> by Lambda
        /// </summary>
        /// <param name="typePredicate"></param>
        /// <param name="flags"></param>
        /// <param name="methodPredicate"></param>
        /// <returns></returns>
        public static MethodInfo GetEftMethod(Func<Type, bool> typePredicate, BindingFlags flags,
            Func<MethodInfo, bool> methodPredicate)
        {
            return GetEftMethod(GetEftType(typePredicate), flags, methodPredicate);
        }

        #endregion

        #region TryGetMethod

        /// <summary>
        ///     Try Find Single <see cref="MethodInfo" /> by Lambda
        /// </summary>
        /// <param name="type"></param>
        /// <param name="flags"></param>
        /// <param name="methodPredicate"></param>
        /// <param name="eftMethod"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool TryGetEftMethod(Type type, BindingFlags flags, Func<MethodInfo, bool> methodPredicate,
            out MethodInfo eftMethod)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            eftMethod = type.GetMethods(flags).SingleOrDefault(methodPredicate);

            return eftMethod != null;
        }

        /// <summary>
        ///     Try Find Single <see cref="MethodInfo" /> by Lambda
        /// </summary>
        /// <param name="typePredicate"></param>
        /// <param name="flags"></param>
        /// <param name="methodPredicate"></param>
        /// <param name="eftMethod"></param>
        /// <returns></returns>
        public static bool TryGetEftMethod(Func<Type, bool> typePredicate, BindingFlags flags,
            Func<MethodInfo, bool> methodPredicate, out MethodInfo eftMethod)
        {
            if (TryGetEftType(typePredicate, out var type))
            {
                return TryGetEftMethod(type, flags, methodPredicate, out eftMethod);
            }
            else
            {
                eftMethod = null;
                return false;
            }
        }

        #endregion

        /// <summary>
        ///     If <see cref="MethodBase" /> is Async then return <see langword="true" />
        /// </summary>
        /// <param name="methodBase"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool IsAsync(this MethodBase methodBase)
        {
            if (methodBase == null)
            {
                throw new ArgumentNullException(nameof(methodBase));
            }

            return methodBase.IsDefined(typeof(AsyncStateMachineAttribute));
        }

        #region IsCompilerGenerated

        /// <summary>
        ///     If <see cref="Type" /> is Compiler Generate then return <see langword="true" />
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool IsCompilerGenerated(this Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return type.IsDefined(typeof(CompilerGeneratedAttribute));
        }

        /// <summary>
        ///     If <see cref="MethodBase" /> is Compiler Generate then return <see langword="true" />
        /// </summary>
        /// <param name="methodBase"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool IsCompilerGenerated(this MethodBase methodBase)
        {
            if (methodBase == null)
            {
                throw new ArgumentNullException(nameof(methodBase));
            }

            return methodBase.IsDefined(typeof(CompilerGeneratedAttribute));
        }

        #endregion

        /// <summary>
        ///     Get Async Struct from <see cref="MethodBase" />
        /// </summary>
        /// <param name="methodBase"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
        public static Type GetAsyncStruct(MethodBase methodBase)
        {
            if (methodBase == null)
            {
                throw new ArgumentNullException(nameof(methodBase));
            }

            var asyncAttribute = methodBase.GetCustomAttribute<AsyncStateMachineAttribute>();

            if (asyncAttribute == null)
            {
                throw new Exception($"{methodBase.Name} not is async type");
            }

            return asyncAttribute.StateMachineType;
        }

        #region GetAsyncMoveNext

        // ReSharper disable once InvalidXmlDocComment
        /// <summary>
        ///     Get Async MoveNext from <see cref="MethodBase" />, Harmony have been added this feature on Newer versions
        ///     <see cref="AccessTools.AsyncMoveNext" />
        /// </summary>
        /// <param name="methodBase"></param>
        /// <returns></returns>
        public static MethodInfo GetAsyncMoveNext(MethodBase methodBase)
        {
            var asyncStruct = GetAsyncStruct(methodBase);

            return asyncStruct.GetMethod("MoveNext", BindingFlags.DeclaredOnly | NonPublic);
        }

        /// <summary>
        ///     Get Async MoveNext from Struct
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static MethodInfo GetAsyncMoveNext(Type type)
        {
            if (!type.IsValueType)
            {
                throw new Exception($"{type.Name} not is Struct");
            }

            var method = type.GetMethod("MoveNext", NonPublic);

            return method == null ? throw new Exception($"{type.Name} not have MoveNext Method") : method;
        }

        #endregion

        /// <summary>
        ///     If <see cref="MethodBase" /> is Async then return <see cref="GetAsyncMoveNext(MethodBase)" />
        /// </summary>
        /// <param name="methodBase"></param>
        /// <returns></returns>
        public static MethodInfo GetRealMethod(this MethodBase methodBase)
        {
            return methodBase.IsAsync() ? GetAsyncMoveNext(methodBase) : (MethodInfo)methodBase;
        }

        /// <summary>
        ///     Get Nested Methods from <see cref="MethodBase" />
        /// </summary>
        /// <param name="methodBase"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IEnumerable<MethodInfo> GetNestedMethods(MethodBase methodBase)
        {
            if (methodBase == null)
            {
                throw new ArgumentNullException(nameof(methodBase));
            }

            var declaringType = methodBase.DeclaringType;
            if (declaringType == null)
            {
                throw new ArgumentNullException(nameof(declaringType));
            }

            return declaringType.GetMethods(DeclaredStatic | NonPublic)
                .Where(x => x.IsAssembly && x.Name.StartsWith($"<{methodBase.Name}>"));
        }

        /// <summary>
        ///     If <see cref="MethodBase" /> Contains this IL then return <see langword="true" />
        /// </summary>
        /// <param name="methodBase"></param>
        /// <param name="opcode"></param>
        /// <param name="operand"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool ContainsIL(this MethodBase methodBase, OpCode opcode, object operand = null)
        {
            if (methodBase == null)
            {
                throw new ArgumentNullException(nameof(methodBase));
            }

            return PatchProcessor.ReadMethodBody(GetRealMethod(methodBase))
                .Any(il => il.Key == opcode && il.Value == operand);
        }

        /// <summary>
        ///     Find all <see cref="OpCodes.Call " /> or <see cref="OpCodes.Callvirt" /> Methods from <see cref="MethodBase" />
        /// </summary>
        /// <param name="methodBase"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static MethodInfo[] GetCallMethods(this MethodBase methodBase)
        {
            if (methodBase == null)
            {
                throw new ArgumentNullException(nameof(methodBase));
            }

            return PatchProcessor.ReadMethodBody(GetRealMethod(methodBase))
                .Where(x => x.Key == OpCodes.Call || x.Key == OpCodes.Callvirt).Select(x => (MethodInfo)x.Value)
                .ToArray();
        }

        /// <summary>
        ///     Try Find BepInEx Plugin <see cref="Type" />
        /// </summary>
        /// <param name="pluginGUID"></param>
        /// <param name="plugin"></param>
        /// <returns></returns>
        public static bool TryGetPlugin(string pluginGUID, out BaseUnityPlugin plugin)
        {
            var hasPluginInfo = Chainloader.PluginInfos.TryGetValue(pluginGUID, out var pluginInfo);

            plugin = hasPluginInfo ? pluginInfo.Instance : null;

            return hasPluginInfo;
        }

        #region GetPluginType

        /// <summary>
        ///     Find <see cref="Type" /> from Plugin by Path
        /// </summary>
        /// <param name="plugin"></param>
        /// <param name="typePath"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static Type GetPluginType(BaseUnityPlugin plugin, string typePath)
        {
            if (plugin == null)
            {
                throw new ArgumentNullException(nameof(plugin));
            }

            return plugin.GetType().Assembly.GetType(typePath, true);
        }

        /// <summary>
        ///     Find Plugin <see cref="Type" /> by Lambda
        /// </summary>
        /// <param name="plugin"></param>
        /// <param name="typePredicate"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static Type GetPluginType(BaseUnityPlugin plugin, Func<Type, bool> typePredicate)
        {
            if (plugin == null)
            {
                throw new ArgumentNullException(nameof(plugin));
            }

            return plugin.GetType().Assembly.GetTypes().Single(typePredicate);
        }

        #endregion

        #region TryGetPluginType

        /// <summary>
        ///     Try Find <see cref="Type" /> from Plugin by Path
        /// </summary>
        /// <param name="plugin"></param>
        /// <param name="typePath"></param>
        /// <param name="pluginType"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool TryGetPluginType(BaseUnityPlugin plugin, string typePath, out Type pluginType)
        {
            if (plugin == null)
            {
                throw new ArgumentNullException(nameof(plugin));
            }

            pluginType = plugin.GetType().Assembly.GetType(typePath);

            return pluginType != null;
        }

        /// <summary>
        ///     Try Find Plugin <see cref="Type" /> by Lambda
        /// </summary>
        /// <param name="plugin"></param>
        /// <param name="typePredicate"></param>
        /// <param name="pluginType"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static bool TryGetPluginType(BaseUnityPlugin plugin, Func<Type, bool> typePredicate, out Type pluginType)
        {
            if (plugin == null)
            {
                throw new ArgumentNullException(nameof(plugin));
            }

            pluginType = plugin.GetType().Assembly.GetTypes().SingleOrDefault(typePredicate);

            return pluginType != null;
        }

        #endregion
    }
}