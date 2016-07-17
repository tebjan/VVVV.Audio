/*
 * Created by SharpDevelop.
 * User: TF
 * Date: 24.06.2015
 * Time: 02:39
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using VVVV.Audio;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Nodes
 {
    [PluginInfo(Name = "Gist", Category = "VAudio", Version = "Sink", Help = "Tracks several features of the incoming audio", AutoEvaluate = true, Tags = "Analysis, FFT, ", Credits = "Adam Stark" )]
    public class GistNode : AutoAudioSinkSignalNode<GistSignal>
    {
        private class UnsafeNativeMethods
        {
            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool SetDllDirectory(string lpPathName);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern int GetDllDirectory(int bufsize, StringBuilder buf);

            [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern IntPtr LoadLibrary(string librayName);
        }

        public static string CoreAssemblyNativeDir
        {
            get
            {
                //get the full location of the assembly with DaoTests in it
                string fullPath = Assembly.GetAssembly(typeof(AudioEngine)).Location;
                var subfolder = Environment.Is64BitProcess ? "x64" : "x86";

                //get the folder that's in
                return Path.Combine(Path.GetDirectoryName(fullPath), subfolder);
            }
        }

        public void LoadDllFile(string dllfolder, string libname)
        {
            var currentpath = new StringBuilder(255);
            var length = UnsafeNativeMethods.GetDllDirectory(currentpath.Length, currentpath);

            // use new path
            var success = UnsafeNativeMethods.SetDllDirectory(dllfolder);

            if (success)
            {
                var handle = UnsafeNativeMethods.LoadLibrary(libname);
                success = handle.ToInt64() > 0;
            }

            // restore old path
            UnsafeNativeMethods.SetDllDirectory(currentpath.ToString());
        }

        public GistNode()
        {
            //Load Gist.dll
            LoadDllFile(CoreAssemblyNativeDir, "Gist.dll");
        }
    }
 }

