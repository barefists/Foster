﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;

namespace Foster.Framework
{

    public abstract class System : Module
    {

        /// <summary>
        /// Underlying System implementation API Name
        /// </summary>
        public string ApiName { get; protected set; } = "Unknown";

        /// <summary>
        /// Underlying System implementation API Version
        /// </summary>
        public Version ApiVersion { get; protected set; } = new Version(0, 0, 0);

        /// <summary>
        /// Whether the System can support Multiple Windows
        /// </summary>
        public abstract bool SupportsMultipleWindows { get; }

        /// <summary>
        /// A list of all opened Windows
        /// </summary>
        public readonly ReadOnlyCollection<Window> Windows;

        /// <summary>
        /// A list of active Monitors
        /// </summary>
        public readonly ReadOnlyCollection<Monitor> Monitors;

        /// <summary>
        /// A list of all the Rendering Contexts
        /// </summary>
        public readonly ReadOnlyCollection<Context> Contexts;

        /// <summary>
        /// System Input
        public readonly Input Input;

        /// <summary>
        /// The application directory
        /// </summary>
        public virtual string Directory => AppDomain.CurrentDomain?.BaseDirectory ?? "";

        /// <summary>
        /// Gets a Pointer to a Platform rendering method of the given name
        /// This is used internally by the Graphics
        /// </summary>
        public abstract IntPtr GetProcAddress(string name);

        /// <summary>
        /// Internal list of all Windows owned by the System. The Platform implementation should maintain this.
        /// </summary>
        protected readonly List<Window> windows = new List<Window>();

        /// <summary>
        /// Internal list of all Monitors owned by the System. The Platform implementation should maintain this.
        /// </summary>
        protected readonly List<Monitor> monitors = new List<Monitor>();

        /// <summary>
        /// Internal list of all Contexts owned by the System. The Platform implementation should maintain this.
        /// </summary>
        protected readonly List<Context> contexts = new List<Context>();

        protected System(Input input) : base(100)
        {
            Windows = new ReadOnlyCollection<Window>(windows);
            Monitors = new ReadOnlyCollection<Monitor>(monitors);
            Contexts = new ReadOnlyCollection<Context>(contexts);
            Input = input;
        }

        protected internal override void Startup()
        {
            Console.WriteLine($" - System {ApiName} {ApiVersion}");
        }

        protected internal override void BeforeUpdate()
        {
            Input.BeforeUpdate();
        }

        protected internal override void Shutdown()
        {
            Console.WriteLine($" - System {ApiName} {ApiVersion} : Shutdown");
        }

        /// <summary>
        /// Creates a new Window. This must be called from the Main Thread.
        /// </summary>
        public Window CreateWindow(string title, int width, int height, WindowFlags flags = WindowFlags.None)
        {
            return CreateWindow(App.Graphics, title, width, height, flags);
        }

        /// <summary>
        /// Creates a new Window. This must be called from the Main Thread.
        /// </summary>
        public abstract Window CreateWindow(Graphics graphics, string title, int width, int height, WindowFlags flags = WindowFlags.None);

        /// <summary>
        /// Creates a new Rendering Context. This must be called from the Main Thread.
        /// </summary>
        public abstract Context CreateContext();

        /// <summary>
        /// Gets the current Rendering Context on the current Thread
        /// </summary>
        public Context? GetCurrentContext()
        {
            var threadId = Thread.CurrentThread.ManagedThreadId;

            for (int i = 0; i < Contexts.Count; i++)
                if (Contexts[i].ActiveThreadId == threadId)
                    return Contexts[i];

            return null;
        }

        /// <summary>
        /// Sets the current Rendering Context on the current Thread
        /// Note that this will fail if the context is current on another thread
        /// </summary>
        public void SetCurrentContext(Context? context)
        {
            // context is already set on this thread
            if (context != null && context.ActiveThreadId == Thread.CurrentThread.ManagedThreadId)
                return;

            // unset existing context
            {
                var current = GetCurrentContext();
                if (current != null)
                    current.ActiveThreadId = 0;
            }

            if (context != null)
            {
                if (context.Disposed)
                    throw new Exception("The Context is Disposed");

                // currently assigned to a different thread
                if (context.ActiveThreadId != 0)
                    throw new Exception("The Context is active on another Thread. A Context can only be current for a single Thread at a time. You must make it non-current on the old Thread before making setting it on another.");

                context.ActiveThreadId = Thread.CurrentThread.ManagedThreadId;
                SetCurrentContextInternal(context);
                App.Modules.ContextChanged(context);
            }
            else
            {
                SetCurrentContextInternal(null);
            }
        }

        /// <summary>
        /// Sets the current Rendering Context on the current Thread
        /// </summary>
        protected abstract void SetCurrentContextInternal(Context? context);

    }
}
