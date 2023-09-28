#region License
/*
    Copyright (c) 2015, Paweł Hofman (CodeTitans)
    All Rights Reserved.

    Licensed under MIT License
    For more information please visit:

    https://github.com/phofman/zip/blob/master/LICENSE
        or
    http://opensource.org/licenses/MIT


    For latest source code, documentation, samples
    and more information please visit:

    https://github.com/phofman/zip
*/
#endregion

using System.Reflection;
using System.Threading;

namespace System.IO.Compression
{
    /// <summary>
    /// Helper class for some Shell32 operations.
    /// </summary>
    static class ShellHelper
    {
        /// <summary>
        /// Simple wrapper class for making reflection easier.
        /// </summary>
        public class ReflectionWrapper
        {
            private readonly object _o;

            /// <summary>
            /// Init constructor.
            /// </summary>
            protected ReflectionWrapper(object o)
            {
                if (o == null)
                    throw new ArgumentNullException("o");

                _o = o;
            }

            /// <summary>
            /// Gets the COM type of the wrapped object.
            /// </summary>
            protected Type WrappedType
            {
                get { return _o.GetType(); }
            }

            /// <summary>
            /// Gets the wrapped object value.
            /// </summary>
            protected internal object WrappedObject
            {
                get { return _o; }
            }

            /// <summary>
            /// Invokes the method with specified name.
            /// </summary>
            protected T InvokeMethod<T>(string name, params object[] args)
            {
                return (T) WrappedType.InvokeMember(name, BindingFlags.InvokeMethod, null, WrappedObject, args != null && args.Length == 0 ? null : args);
            }

            /// <summary>
            /// Invokes the method with specified name.
            /// </summary>
            protected object InvokeMethod(string name, params object[] args)
            {
                return WrappedType.InvokeMember(name, BindingFlags.InvokeMethod, null, WrappedObject, args != null && args.Length == 0 ? null : args);
            }

            /// <summary>
            /// Gets the value of specified property.
            /// </summary>
            protected T GetProperty<T>(string name)
            {
                return (T) WrappedType.InvokeMember(name, BindingFlags.GetProperty, null, WrappedObject, null);
            }

            /// <summary>
            /// Sets the value of specified property.
            /// </summary>
            protected void SetProperty(string name, object value)
            {
                WrappedType.InvokeMember(name, BindingFlags.SetProperty, null, WrappedObject, new object[] { value });
            }
        }

        /// <summary>
        /// Wrapper class for the Shell Folder COM class.
        /// https://msdn.microsoft.com/en-us/library/windows/desktop/bb787868(v=vs.85).aspx
        /// </summary>
        public class Folder : ReflectionWrapper
        {
            public Folder(object o, string path)
                : base(o)
            {
                Path = path;
            }

            #region Properties

            /// <summary>
            /// Full path represented by this folder (or ZIP archive).
            /// </summary>
            public string Path
            {
                get;
                private set;
            }

            #endregion

            /// <summary>
            /// Returns a string that represents the current object.
            /// </summary>
            /// <returns>
            /// A string that represents the current object.
            /// </returns>
            public override string ToString()
            {
                return Path;
            }

            public FolderItems Items()
            {
                return new FolderItems(InvokeMethod("Items"));
            }

            /// <summary>
            /// Copies specified items (single file or collection) into current folder or ZIP archive.
            /// </summary>
            public void Copy(ReflectionWrapper items)
            {
                const int NoProgressDialog = 4;
                const int RespondYesToAllDialogs = 16;
                const int NoUiOnError = 1024;

                // HINT: somehow flags about UI are ignored and if operation takes a bit more time (from several seconds up)
                //       shell will display the progress with option to cancel
                // HINT: this call is asynchronous and starts another thread without any way to easily monitor progress
                InvokeMethod("CopyHere", items.WrappedObject, NoProgressDialog | RespondYesToAllDialogs | NoUiOnError);
            }
        }

        /// <summary>
        /// Wrapper class for the Shell FolderItems COM class.
        /// https://msdn.microsoft.com/en-us/library/windows/desktop/bb787800(v=vs.85).aspx
        /// </summary>
        public class FolderItems : ReflectionWrapper
        {
            public FolderItems(object o)
                : base(o)
            {
            }

            /// <summary>
            /// Gets the number of items.
            /// </summary>
            public int Count
            {
                get { return GetProperty<int>("Count"); }
            }

            /// <summary>
            /// Gets item at specified index.
            /// </summary>
            public FolderItem this[int index]
            {
                get { return new FolderItem(InvokeMethod("Item", index)); }
            }
        }

        /// <summary>
        /// Wrapper class for the Shell FolderItem COM class.
        /// https://msdn.microsoft.com/en-us/library/windows/desktop/bb787810(v=vs.85).aspx
        /// </summary>
        public class FolderItem : ReflectionWrapper
        {
            public FolderItem(object o)
                : base(o)
            {
            }

            /// <summary>
            /// Checks if given item is a folder.
            /// </summary>
            public bool IsFolder
            {
                get { return GetProperty<bool>("IsFolder"); }
            }

            /// <summary>
            /// Gets or sets the name.
            /// </summary>
            public string Name
            {
                get { return GetProperty<string>("Name"); }
                set { SetProperty("Name", value); }
            }

            /// <summary>
            /// Gets the size.
            /// </summary>
            public long Size
            {
                get { return GetProperty<int>("Size"); }
            }

            /// <summary>
            /// Gets the full path of an item. If it's inside the ZIP archive, it will be prefixed with the path to that ZIP.
            /// </summary>
            public string Path
            {
                get { return GetProperty<string>("Path"); }
            }

            /// <summary>
            /// Gets the folder representation of an item (if it's actually a folder) or null.
            /// </summary>
            public Folder AsFolder
            {
                get
                {
                    if (IsFolder)
                    {
                        return new Folder(GetProperty<object>("GetFolder"), Path);
                    }

                    return null;
                }
            }

            /// <summary>
            /// Returns a string that represents the current object.
            /// </summary>
            /// <returns>
            /// A string that represents the current object.
            /// </returns>
            public override string ToString()
            {
                return Path;
            }
        }

        /// <summary>
        /// Gets the folder wrapper representation for specified path.
        /// </summary>
        public static Folder GetShell32Folder(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException("path");
            if (!Directory.Exists(path) && !File.Exists(path))
                throw new ArgumentOutOfRangeException("path", "Requested path doesn't exist");

            var shellAppType = Type.GetTypeFromProgID("Shell.Application");
            var shell = Activator.CreateInstance(shellAppType);
            return new Folder(shellAppType.InvokeMember("NameSpace", BindingFlags.InvokeMethod, null, shell, new object[] { path }), path);
        }

        /// <summary>
        /// Waits until specified file is not in use.
        /// </summary>
        public static void WaitForCompletion(string fileName)
        {
            Thread.Sleep(300);
            while (File.Exists(fileName) && IsInUse(fileName))
            {
                Thread.Sleep(100);
            }
        }

        private static bool IsInUse(string filePath)
        {
            try
            {
                var file = File.OpenRead(filePath);
                file.Close();
                return false;
            }
            catch
            {
                return true;
            }
        }

    }
}
