﻿using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace TaskScheduler_HealthCheck
{
    class Program
    {
        // This program tries to find the cause of Windows Event log`s ERROR ID: 413 (Task Scheduler service failed to load tasks at service startup. Additional Data: Error Value: 2147942402)
        // As this link suggests:
        //https://social.technet.microsoft.com/Forums/windows/en-US/1f677dd3-bdb7-4650-9164-d8e2c66b7708/task-scheduler-error?forum=w7itprogeneral
        // this program compare 2 palaces of Windows registry where Task`s information is stored
        // hope we can figure out the problem with looking at the differece of Tasks in these 2 places
        const string REGISTRYPATH = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache";
        private static HashSet<string> _tasksUnderTree; // all tasks that are exist under: REGISTRYPATH + "\Tree"
        private static HashSet<string> _tasksUnderTasks; // all tasks that are exist under: REGISTRYPATH + "\Tasks"
        private static List<string> _orphandEntries;

        static void Main(string[] args)
        {
            _tasksUnderTree = new HashSet<string>();
            _tasksUnderTasks = new HashSet<string>();

            using (RegistryKey r = OpenSubKey(REGISTRYPATH + @"\Tree"))
                LoadFromRegistryForTree(r);

            _tasksUnderTasks = LoadFromRegistryForTasks();
            Console.WriteLine(@"Count of Task enteries under \Tasks:{0}", _tasksUnderTasks.Count);
            _tasksUnderTasks.ExceptWith(_tasksUnderTree);
            ConsoleColor oldForeground = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(@"Task enteries which exist under \Tasks but not under \Tree:");
            Console.ForegroundColor = oldForeground;
            foreach (var t in _tasksUnderTasks)
                Console.WriteLine(" {0}", t);
            Console.WriteLine("");
            _tasksUnderTasks = LoadFromRegistryForTasks();
            _tasksUnderTree.ExceptWith(_tasksUnderTasks);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(@"Task enteries which exist under \Tree but not under \Tasks:");
            Console.ForegroundColor = oldForeground;
            foreach (var t in _tasksUnderTree)
                Console.WriteLine(" {0}", t);

            Console.WriteLine("");

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(@"Registry keys under \Tasks with no information within them (Must be deleted):");
            Console.ForegroundColor = oldForeground;
            foreach (var t in _orphandEntries)
                Console.WriteLine(" {0}", t);
            Console.ReadKey();
        }




        /// <summary>
        /// Reading all subkeys exists under: REGISTRYPATH+"\Tree" 
        /// and writing their names to: _tasksUnderTree 
        /// </summary>
        private static void LoadFromRegistryForTree(RegistryKey reg)
        {
            if (reg.SubKeyCount == 0)
            {
                string path = reg.Name.Substring(87);
                _tasksUnderTree.Add(path);
            }
            else
            {
                string[] subKeyNames = reg.GetSubKeyNames();
                foreach (var subKeyName in subKeyNames)
                {
                    var subKeyFullName = reg.Name.Substring(19) + @"\" + subKeyName;
                    using (var r = OpenSubKey(subKeyFullName))
                        LoadFromRegistryForTree(r); // recursive
                }
            }
        }

        private static HashSet<string> LoadFromRegistryForTasks()
        {
            var h = new HashSet<string>();
            _orphandEntries = new List<string>();
            using (var r = OpenSubKey(REGISTRYPATH + @"\Tasks"))
            {
                string[] subKeyNames = r.GetSubKeyNames();
                foreach (var subKeyName in subKeyNames)
                {
                    var subKeyFullName = r.Name + @"\" + subKeyName;
                    var v = Registry.GetValue(subKeyFullName, "Path", null);
                    if (v == null)
                        _orphandEntries.Add(subKeyFullName);
                    else
                        h.Add(v.ToString());
                }
            }

            return h;
        }

        private static RegistryKey OpenSubKey(string subKeyFullName)
        {
            try
            {
                return Registry.LocalMachine.OpenSubKey(subKeyFullName);
            }
            catch (Exception e)
            {
                throw new ApplicationException("Failed to open registry key \"" + subKeyFullName + "\"", e);
            }
        }
    }
}
