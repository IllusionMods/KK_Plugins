using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MaterialEditorAPI
{
    /// <summary>
    /// Stable entry point for Material Editor extension capabilities.
    /// </summary>
    public static class MaterialEditorExtensionApi
    {
        private static readonly Version Version = new Version(1, 0, 0);

        /// <summary>Current semantic extension API version.</summary>
        public static Version ApiVersion => Version;

        /// <summary>Capabilities implemented by this Material Editor build.</summary>
        public static MaterialEditorApiCapability Capabilities =>
            MaterialEditorApiCapability.LabelClickEvents
            | MaterialEditorApiCapability.SelectionEvents
            | MaterialEditorApiCapability.PropertyDescriptorProviders
            | MaterialEditorApiCapability.PropertyEditors
            | MaterialEditorApiCapability.EditServiceFacade;

        /// <summary>Check whether every requested capability is available.</summary>
        public static bool Supports(MaterialEditorApiCapability capabilities) =>
            (Capabilities & capabilities) == capabilities;

        /// <summary>Register a semantic selection handler.</summary>
        public static IDisposable RegisterSelectionHandler(
            Action<MaterialEditorSelectionEventArgs> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            return MaterialEditorExtensionRegistry.RegisterSelectionHandler(handler);
        }

        /// <summary>Unregister a semantic selection handler.</summary>
        public static void UnregisterSelectionHandler(
            Action<MaterialEditorSelectionEventArgs> handler)
        {
            MaterialEditorExtensionRegistry.UnregisterSelectionHandler(handler);
        }

        /// <summary>
        /// Register a provider of additional material property descriptors.
        /// Dispose the returned registration when the provider plugin is unloaded.
        /// </summary>
        public static IDisposable RegisterPropertyDescriptorProvider(
            string ownerId,
            MaterialEditorPropertyDescriptorProvider provider,
            int priority = 0)
        {
            return MaterialEditorExtensionRegistry.RegisterPropertyDescriptorProvider(
                ownerId,
                provider,
                priority);
        }

        /// <summary>
        /// Register a semantic property editor factory.
        /// Built-in editor IDs cannot be replaced.
        /// </summary>
        public static IDisposable RegisterPropertyEditor(
            string ownerId,
            string editorId,
            MaterialEditorPropertyEditorFactory factory)
        {
            return MaterialEditorExtensionRegistry.RegisterPropertyEditor(
                ownerId,
                editorId,
                factory);
        }

        /// <summary>
        /// Get an edit service bound to a root object and its Material Editor storage context.
        /// Returns null before Material Editor has initialized.
        /// </summary>
        public static MaterialEditorEditService GetEditService(
            GameObject gameObject,
            object data)
        {
            return MaterialEditorExtensionRegistry.CreateEditService(gameObject, data);
        }
    }

    internal static class MaterialEditorExtensionRegistry
    {
        private sealed class ProviderRegistration
        {
            internal string OwnerId;
            internal int Priority;
            internal long Sequence;
            internal MaterialEditorPropertyDescriptorProvider Provider;
        }

        private sealed class EditorRegistration
        {
            internal string OwnerId;
            internal string EditorId;
            internal MaterialEditorPropertyEditorFactory Factory;
        }

        private sealed class Registration : IDisposable
        {
            private Action _dispose;

            internal Registration(Action dispose)
            {
                _dispose = dispose;
            }

            public void Dispose()
            {
                var dispose = _dispose;
                if (dispose == null)
                    return;
                _dispose = null;
                dispose();
            }
        }

        private static readonly object Sync = new object();
        private static readonly List<Action<MaterialEditorSelectionEventArgs>> SelectionHandlers =
            new List<Action<MaterialEditorSelectionEventArgs>>();
        private static readonly List<ProviderRegistration> DescriptorProviders =
            new List<ProviderRegistration>();
        private static readonly Dictionary<string, EditorRegistration> EditorFactories =
            new Dictionary<string, EditorRegistration>(StringComparer.Ordinal);
        private static long _registrationSequence;
        private static MaterialEditService _activeEditService;

        internal static void SetActiveEditService(MaterialEditService editService)
        {
            _activeEditService = editService;
        }

        internal static MaterialEditorEditService CreateEditService(
            GameObject gameObject,
            object data)
        {
            var service = _activeEditService;
            return service == null
                ? null
                : new MaterialEditorEditService(service, gameObject, data);
        }

        internal static MaterialEditorTargetContext CreateTargetContext(
            MaterialEditService service,
            GameObject gameObject,
            object data,
            Renderer renderer,
            Material material,
            Projector projector)
        {
            var facade = service == null
                ? CreateEditService(gameObject, data)
                : new MaterialEditorEditService(service, gameObject, data);
            return new MaterialEditorTargetContext(
                gameObject,
                data,
                renderer,
                material,
                projector,
                facade);
        }

        internal static IDisposable RegisterSelectionHandler(
            Action<MaterialEditorSelectionEventArgs> handler)
        {
            lock (Sync)
            {
                if (!SelectionHandlers.Contains(handler))
                    SelectionHandlers.Add(handler);
            }
            return new Registration(() => UnregisterSelectionHandler(handler));
        }

        internal static void UnregisterSelectionHandler(
            Action<MaterialEditorSelectionEventArgs> handler)
        {
            if (handler == null)
                return;
            lock (Sync)
                SelectionHandlers.Remove(handler);
        }

        internal static void RaiseSelection(MaterialEditorSelectionEventArgs eventArgs)
        {
            Action<MaterialEditorSelectionEventArgs>[] handlers;
            lock (Sync)
                handlers = SelectionHandlers.ToArray();

            foreach (var handler in handlers)
            {
                try
                {
                    handler(eventArgs);
                }
                catch (Exception ex)
                {
                    LogError("selection handler", ex);
                }
            }
        }

        internal static void RaiseSelection(
            MaterialEditService service,
            MaterialEditorSelectionType selectionType,
            MaterialEditorSelectionAction action,
            string name,
            GameObject gameObject,
            object data,
            Renderer renderer,
            Material material,
            Projector projector,
            MaterialEditorPropertyDescriptor property = null)
        {
            RaiseSelection(
                new MaterialEditorSelectionEventArgs(
                    selectionType,
                    action,
                    name,
                    CreateTargetContext(
                        service,
                        gameObject,
                        data,
                        renderer,
                        material,
                        projector),
                    property));
        }

        internal static void RaiseRowSelection(
            RowModel row,
            MaterialEditorSelectionType selectionType,
            MaterialEditorSelectionAction action,
            string name)
        {
            RaiseSelection(
                null,
                selectionType,
                action,
                name,
                row.GameObject,
                row.Data,
                row.Renderer,
                row.Material,
                row.Projector,
                row.PublicDescriptor);
        }

        internal static void RaiseLabelSelection(
            RowModel row,
            MaterialEditorLabelType labelType,
            string name)
        {
            MaterialEditorSelectionType selectionType;
            switch (labelType)
            {
                case MaterialEditorLabelType.Renderer:
                    selectionType = MaterialEditorSelectionType.Renderer;
                    break;
                case MaterialEditorLabelType.Material:
                    selectionType = MaterialEditorSelectionType.Material;
                    break;
                case MaterialEditorLabelType.Shader:
                    selectionType = MaterialEditorSelectionType.Shader;
                    break;
                default:
                    selectionType = MaterialEditorSelectionType.Property;
                    break;
            }
            RaiseRowSelection(
                row,
                selectionType,
                MaterialEditorSelectionAction.Activated,
                name);
        }

        internal static IDisposable RegisterPropertyDescriptorProvider(
            string ownerId,
            MaterialEditorPropertyDescriptorProvider provider,
            int priority)
        {
            ValidateOwner(ownerId);
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            var registration = new ProviderRegistration
            {
                OwnerId = ownerId,
                Priority = priority,
                Provider = provider
            };
            lock (Sync)
            {
                registration.Sequence = _registrationSequence++;
                DescriptorProviders.Add(registration);
            }
            return new Registration(() =>
            {
                lock (Sync)
                    DescriptorProviders.Remove(registration);
            });
        }

        internal static IDisposable RegisterPropertyEditor(
            string ownerId,
            string editorId,
            MaterialEditorPropertyEditorFactory factory)
        {
            ValidateOwner(ownerId);
            if (string.IsNullOrEmpty(editorId))
                throw new ArgumentException("An editor ID is required.", nameof(editorId));
            if (IsBuiltInEditor(editorId))
                throw new ArgumentException("Built-in Material Editor property editors cannot be replaced.", nameof(editorId));
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            var registration = new EditorRegistration
            {
                OwnerId = ownerId,
                EditorId = editorId,
                Factory = factory
            };
            lock (Sync)
            {
                if (EditorFactories.ContainsKey(editorId))
                    throw new InvalidOperationException($"A property editor is already registered for '{editorId}'.");
                EditorFactories.Add(editorId, registration);
            }
            return new Registration(() =>
            {
                lock (Sync)
                {
                    EditorRegistration current;
                    if (EditorFactories.TryGetValue(editorId, out current)
                        && ReferenceEquals(current, registration))
                        EditorFactories.Remove(editorId);
                }
            });
        }

        internal static IList<MaterialEditorPropertyDescriptor> GetPropertyDescriptors(
            MaterialEditorPropertyContext context)
        {
            ProviderRegistration[] providers;
            lock (Sync)
            {
                providers = DescriptorProviders
                    .OrderByDescending(entry => entry.Priority)
                    .ThenBy(entry => entry.Sequence)
                    .ToArray();
            }

            var result = new List<MaterialEditorPropertyDescriptor>();
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var registration in providers)
            {
                IEnumerable<MaterialEditorPropertyDescriptor> descriptors;
                try
                {
                    var provided = registration.Provider(context);
                    descriptors = provided == null
                        ? null
                        : provided.ToList();
                }
                catch (Exception ex)
                {
                    LogError($"property descriptor provider '{registration.OwnerId}'", ex);
                    continue;
                }

                if (descriptors == null)
                    continue;
                foreach (var descriptor in descriptors)
                {
                    if (descriptor == null)
                        continue;
                    var key = registration.OwnerId + ":" + descriptor.Id;
                    if (!keys.Add(key))
                    {
                        LogWarning($"Duplicate extension property '{key}' was ignored.");
                        continue;
                    }
                    if (string.IsNullOrEmpty(descriptor.DisplayName))
                        descriptor.DisplayName = descriptor.Id;
                    if (string.IsNullOrEmpty(descriptor.PropertyName))
                        descriptor.PropertyName = descriptor.Id;
                    if (descriptor.Category == null)
                        descriptor.Category = string.Empty;
                    result.Add(descriptor);
                }
            }
            return result;
        }

        internal static MaterialEditorPropertyEditor CreatePropertyEditor(
            MaterialEditorPropertyContext context,
            MaterialEditorPropertyDescriptor descriptor)
        {
            EditorRegistration registration;
            lock (Sync)
                EditorFactories.TryGetValue(descriptor.EditorId, out registration);
            if (registration == null)
                return null;

            try
            {
                return registration.Factory(context, descriptor);
            }
            catch (Exception ex)
            {
                LogError(
                    $"property editor '{descriptor.EditorId}' from '{registration.OwnerId}'",
                    ex);
                return null;
            }
        }

        internal static bool IsBuiltInEditor(string editorId)
        {
            return editorId == MaterialEditorPropertyEditorIds.Float
                   || editorId == MaterialEditorPropertyEditorIds.Color
                   || editorId == MaterialEditorPropertyEditorIds.Boolean
                   || editorId == MaterialEditorPropertyEditorIds.Texture;
        }

        internal static bool HasPropertyEditor(string editorId)
        {
            if (IsBuiltInEditor(editorId))
                return true;
            lock (Sync)
                return EditorFactories.ContainsKey(editorId);
        }

        private static void ValidateOwner(string ownerId)
        {
            if (string.IsNullOrEmpty(ownerId))
                throw new ArgumentException("A plugin or owner ID is required.", nameof(ownerId));
        }

        private static void LogError(string source, Exception exception)
        {
            MaterialEditorPluginBase.Logger?.LogError(
                $"Exception in Material Editor {source}: {exception}");
        }

        private static void LogWarning(string message)
        {
            MaterialEditorPluginBase.Logger?.LogWarning(message);
        }
    }
}
