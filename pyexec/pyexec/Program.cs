using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Xml.Linq;
using System.Runtime.InteropServices;

namespace pyexec
{
    class Program
    {
        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        static extern int system(string command);
        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        static extern int _putenv(string envstring);
        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl, SetLastError = true)]
        static extern int _chdir(string envstring); 
        
        static string SettingsFilename = "pyexec.config";

        static void Main(string[] args)
        {
            try
            {
                CheckSettingsFile();

                if (args.Length == 0)
                {
                    Console.WriteLine("pyexec.exe --help : For help");
                    return;
                }

                bool showhelp = false;
                bool listenv=false;
                foreach (string arg in args)
                {
                    if (arg.ToLower() == "--help")
                        showhelp = true;
                    if(arg.ToLower() == "--list")
                        listenv = true;
                }
                if (showhelp)
                {
                    Help();
                    return;
                }
                if(listenv)
                {
                    ListEnv();
                    return;
                }




                if (args.Length >= 3)
                {
                    if (args[0].ToLower() == "--env")
                    {
                        RunCommand(args[1], args[2]);
                    }
                    else if (args[0].ToLower() == "--run")
                    {
                        RunFileAs(args[1], args[2]);
                    }
                }
                else if (args.Length >= 1)
                {
                    RunFile(args[0]);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception:\n{0}", ex.Message);
            }
        }

     

        static void Help()
        {
            Console.WriteLine("pyexec.exe");
            Console.WriteLine("In pyexec directory, there is a pyexec.config file which contains the Python environment details.");
            Console.WriteLine("Only specify the filename as argument to execute. The Python file is read and its first non-empty line is loaded.");
            Console.WriteLine("The first line must contain a single string representing the defined environment name of the Python executable");
            Console.WriteLine("--env ENVIRONMENT [COMMAND STRING IN QUOTE]: will execute the command in the environment specified");
            Console.WriteLine("--list : Lists all the environments");
            Console.WriteLine("DEFAULT: the default python enironment [reserved environment name]");
        }

        static void CheckSettingsFile()
        {
            string path = Path.GetDirectoryName(Assembly.GetAssembly(typeof(Program)).CodeBase);
            if (path.ToLower().IndexOf("file:\\") != -1)
            {
                path = path.Substring(6);
            }
            FileInfo finfo = new FileInfo(path+"\\"+SettingsFilename);
            if (!finfo.Exists)
            {
               using (StreamWriter w = new StreamWriter(finfo.Create()))
               {
                   w.WriteLine("<?xml version=\"1.0\"?>");
                   w.WriteLine("<pyexec>");
                   w.WriteLine("\t<default>");
                   w.WriteLine("\t\t<path append='1'></path>");
                   w.WriteLine("\t\t<python>python</python>");
                   w.WriteLine("\t\t<pythonpath append='1'></pythonpath>");
                   w.WriteLine("\t</default>");
                   w.WriteLine("</pyexec>");
                   w.Close();
               }
            }
  
        }

        static void ListEnv()
        {
            string path = Path.GetDirectoryName(Assembly.GetAssembly(typeof(Program)).CodeBase);
            if (path.ToLower().IndexOf("file:\\") != -1)
            {
                path = path.Substring(6);
            }
            FileInfo finfo = new FileInfo(path + "\\" + SettingsFilename);

            XElement parent = XElement.Load(finfo.OpenText());
           
            if (parent.Name.ToString().ToLower() == "pyexec")
            {
                Console.WriteLine("Environment Names:");
                foreach (XElement node in parent.Elements())
                {
                    Console.WriteLine("\t{0}",node.Name.ToString().ToUpper());
                }
            }

        }

        static void RunFile(string filename)
        {
            XElement def_node = null;
            XElement req_node = null;


            FileInfo fi = new FileInfo(filename);
            if (!fi.Exists)
            {
                filename = fi.FullName + ".py";
                fi = new FileInfo(filename);
                if (!fi.Exists)
                {
                    throw new Exception(string.Format("File '{0}' not found", fi.FullName));
                }
            }

            string firstline = "";
            using (StreamReader r = new StreamReader(fi.OpenRead()))
            {
                while (!r.EndOfStream)
                {
                    string line = r.ReadLine().Trim();
                    if (line.Trim().Length < 1)
                    {
                        continue;
                    }
                    else if (line.IndexOf("#") ==0)
                    {
                        firstline = line;
                        break;
                    }
                }
                r.Close();
            }

            string[] arr = firstline.Split(' ');
            string env = arr[1];            
            string environment_PATH = Environment.GetEnvironmentVariable("PATH");
            string environment_PYTHONPATH = Environment.GetEnvironmentVariable("PYTHONPATH");
            string exe_python = "python";

            string path = Path.GetDirectoryName(Assembly.GetAssembly(typeof(Program)).CodeBase);
            if (path.ToLower().IndexOf("file:\\") != -1)
            {
                path = path.Substring(6);
            }
            FileInfo finfo = new FileInfo(path + "\\" + SettingsFilename);

            XElement parent = XElement.Load(finfo.OpenText());

            XElement env_default = null;
            XElement env_required = null;
            if (parent.Name.ToString().ToLower() == "pyexec")
            {
                foreach (XElement node in parent.Elements())
                {
                    if (node.Name.ToString().ToLower() == "default")
                    {
                        def_node = node;
                        env_default = node;
                    }

                    if (node.Name.ToString().ToLower() == env.ToLower())
                    {
                        req_node = node;
                        env_required = node;
                    }
                }
            }

            if (env_required == null)
            {
                env_required = env_default;
            }
            if (env_required != null)
            {
                XElement elpath = env_required.Element("path");
                XElement elpythonpath = env_required.Element("pythonpath");
                XElement elpythonexe = env_required.Element("python");
                if (elpath != null)
                {
                    string v = elpath.Attribute("append").Value;

                    if (v == null)
                        v = "1";
                    if (v == "0")
                        environment_PATH = elpath.Value;
                    else
                        environment_PATH = string.Format("{0};{1}", elpath.Value, environment_PATH);
                }
                if (elpythonpath != null)
                {
                    string v = elpythonpath.Attribute("append").Value;

                    if (v == null)
                        v = "1";
                    if (v == "0")
                        environment_PYTHONPATH = elpythonpath.Value;
                    else
                        environment_PYTHONPATH = string.Format("{0};{1}", elpythonpath.Value, environment_PYTHONPATH);
                }
                if (elpythonexe != null)
                {
                    exe_python = elpythonexe.Value;
                }

            }

            DirectoryInfo di = fi.Directory;

            _putenv(string.Format("PATH={0}", environment_PATH));
            _putenv(string.Format("PYTHONPATH={0}", environment_PYTHONPATH));

            if (req_node != null)
            {
                foreach (XElement ee in req_node.Elements("newpath"))
                {
                    string ev = ee.Attribute("name").Value;
                    string app = ee.Attribute("append").Value;
                    string newpath = ee.Attribute("value").Value;
                    if (app == "1")
                    {
                        string oldpath = Environment.GetEnvironmentVariable(ev);
                        newpath = string.Format("{0};{1}", newpath, oldpath);
                    }
                    _putenv(string.Format("{0}={1}", ev, newpath));
                }

                foreach (XElement bb in req_node.Elements("batch"))
                {
                    string bbcmd = bb.Attribute("name").Value;
                    system(bbcmd);
                }

            }


            _chdir(di.FullName);

            string cmd = string.Format("{0} \"{1}\"", exe_python, Path.GetFileName(fi.FullName));
            //Console.WriteLine(cmd);
            system(cmd);
        }

        static void RunFileAs(string env,string filename)
        {
            XElement def_node = null;
            XElement req_node = null;


            FileInfo fi = new FileInfo(filename);
            if (!fi.Exists)
            {
                filename = fi.FullName + ".py";
                fi = new FileInfo(filename);
                if (!fi.Exists)
                {
                    throw new Exception(string.Format("File '{0}' not found", fi.FullName));
                }
            }

            string environment_PATH = Environment.GetEnvironmentVariable("PATH");
            string environment_PYTHONPATH = Environment.GetEnvironmentVariable("PYTHONPATH");
            string exe_python = "python";

            string path = Path.GetDirectoryName(Assembly.GetAssembly(typeof(Program)).CodeBase);
            if (path.ToLower().IndexOf("file:\\") != -1)
            {
                path = path.Substring(6);
            }
            FileInfo finfo = new FileInfo(path + "\\" + SettingsFilename);

            XElement parent = XElement.Load(finfo.OpenText());

            XElement env_default = null;
            XElement env_required = null;
            if (parent.Name.ToString().ToLower() == "pyexec")
            {
                foreach (XElement node in parent.Elements())
                {
                    if (node.Name.ToString().ToLower() == "default")
                    {
                        def_node = node;
                        env_default = node;
                    }

                    if (node.Name.ToString().ToLower() == env.ToLower())
                    {
                        req_node = node;
                        env_required = node;
                    }
                }
            }

            if (env_required == null)
            {
                env_required = env_default;
            }
            if (env_required != null)
            {
                XElement elpath = env_required.Element("path");
                XElement elpythonpath = env_required.Element("pythonpath");
                XElement elpythonexe = env_required.Element("python");
                if (elpath != null)
                {
                    string v = elpath.Attribute("append").Value;

                    if (v == null)
                        v = "1";
                    if (v == "0")
                        environment_PATH = elpath.Value;
                    else
                        environment_PATH = string.Format("{0};{1}", elpath.Value, environment_PATH);
                }
                if (elpythonpath != null)
                {
                    string v = elpythonpath.Attribute("append").Value;

                    if (v == null)
                        v = "1";
                    if (v == "0")
                        environment_PYTHONPATH = elpythonpath.Value;
                    else
                        environment_PYTHONPATH = string.Format("{0};{1}", elpythonpath.Value, environment_PYTHONPATH);
                }
                if (elpythonexe != null)
                {
                    exe_python = elpythonexe.Value;
                }

            }

            DirectoryInfo di = fi.Directory;

            _putenv(string.Format("PATH={0}", environment_PATH));
            _putenv(string.Format("PYTHONPATH={0}", environment_PYTHONPATH));

            if (req_node != null)
            {
                foreach (XElement ee in req_node.Elements("newpath"))
                {
                    string ev = ee.Attribute("name").Value;
                    string app = ee.Attribute("append").Value;
                    string newpath = ee.Attribute("value").Value;
                    if (app == "1")
                    {
                        string oldpath = Environment.GetEnvironmentVariable(ev);
                        newpath = string.Format("{0};{1}", newpath, oldpath);
                    }
                    _putenv(string.Format("{0}={1}", ev, newpath));
                }

                foreach (XElement bb in req_node.Elements("batch"))
                {
                    string bbcmd = bb.Attribute("name").Value;
                    system(bbcmd);
                }

            }


            _chdir(di.FullName);

            string cmd = string.Format("{0} \"{1}\"", exe_python, Path.GetFileName(fi.FullName));
            //Console.WriteLine(cmd);
            system(cmd);
        }

        static void RunCommand(string env, string cmd)
        {
            string environment_PATH = Environment.GetEnvironmentVariable("PATH");
            string environment_PYTHONPATH = Environment.GetEnvironmentVariable("PYTHONPATH");

            XElement def_node = null;
            XElement req_node = null;

            string path = Path.GetDirectoryName(Assembly.GetAssembly(typeof(Program)).CodeBase);
            if (path.ToLower().IndexOf("file:\\") != -1)
            {
                path = path.Substring(6);
            }
            FileInfo finfo = new FileInfo(path + "\\" + SettingsFilename);

            XElement parent = XElement.Load(finfo.OpenText());

            XElement env_default=null;
            XElement env_required = null;
            if (parent.Name.ToString().ToLower() == "pyexec")
            {
                foreach (XElement node in parent.Elements())
                {
                    if (node.Name.ToString().ToLower() == "default")
                    {
                        env_default = node;
                        def_node = node;
                    }

                    if (node.Name.ToString().ToLower() == env.ToLower())
                    {
                        env_required = node;
                        req_node = node;
                    }
                }
            }

            if (env_required == null)
            {
                env_required = env_default;
            }
            if (env_required != null)
            {
                XElement elpath = env_required.Element("path");
                XElement elpythonpath = env_required.Element("pythonpath");

                if (elpath != null)
                {
                    string v = elpath.Attribute("append").Value;
                    
                    if (v == null)
                        v = "1";
                    if (v == "0")
                        environment_PATH = elpath.Value;
                    else
                        environment_PATH = string.Format("{0};{1}",elpath.Value, environment_PATH);
                }
                if (elpythonpath != null)
                {
                    string v = elpythonpath.Attribute("append").Value;

                    if (v == null)
                        v = "1";
                    if (v == "0")
                        environment_PYTHONPATH = elpythonpath.Value;
                    else
                        environment_PYTHONPATH = string.Format("{0};{1}", elpythonpath.Value, environment_PYTHONPATH);
                }

            }

            if (req_node == null)
            {
                req_node = def_node;
            }

            _putenv(string.Format("PATH={0}", environment_PATH));
            _putenv(string.Format("PYTHONPATH={0}", environment_PYTHONPATH));

            if (req_node != null)
            {
                foreach (XElement ee in req_node.Elements("newpath"))
                {
                    string ev = ee.Attribute("name").Value;
                    string app = ee.Attribute("append").Value;
                    string newpath = ee.Attribute("value").Value;
                    if (app == "1")
                    {
                        string oldpath = Environment.GetEnvironmentVariable(ev);
                        newpath = string.Format("{0};{1}", newpath, oldpath);
                    }
                    _putenv(string.Format("{0}={1}", ev, newpath));
                }

                foreach (XElement bb in req_node.Elements("batch"))
                {
                    string bbcmd = bb.Attribute("name").Value;
                    system(bbcmd);
                }

            }

            system(cmd);
        }
    }
}
