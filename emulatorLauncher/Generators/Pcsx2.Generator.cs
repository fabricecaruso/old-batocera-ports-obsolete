﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;

namespace emulatorLauncher
{
    class Pcsx2Generator : Generator
    {
        public Pcsx2Generator()
        {
            DependsOnDesktopResolution = true;
        }

        public override void RunAndWait(ProcessStartInfo path)
        {
            FakeBezelFrm bezel = null;

            if (_bezelFileInfo != null)
                bezel = _bezelFileInfo.ShowFakeBezel(_resolution);
        
            base.RunAndWait(path);

            if (bezel != null)
                bezel.Dispose();
        }

        private string _path;
        private BezelFiles _bezelFileInfo;
        private ScreenResolution _resolution;

        public override System.Diagnostics.ProcessStartInfo Generate(string system, string emulator, string core, string rom, string playersControllers, ScreenResolution resolution)
        {
            _path = AppConfig.GetFullPath("pcsx2");
            _resolution = resolution;

            string exe = Path.Combine(_path, "pcsx2.exe");
            if (!File.Exists(exe))
                return null;

            SetupPaths(core);
            SetupVM();
            SetupLilyPad();
            SetupGSDx(resolution);

            if (SystemConfig["ratio"] == "4:3" || SystemConfig["ratio"] == "")
                _bezelFileInfo = BezelFiles.GetBezelFiles(system, rom, resolution);

            _resolution = resolution;

            List<string> commandArray = new List<string>();
            commandArray.Add("--portable");
            commandArray.Add("--fullscreen");
            commandArray.Add("--nogui");

            if (SystemConfig.isOptSet("fullboot") && SystemConfig.getOptBoolean("fullboot"))
                commandArray.Add("--fullboot");

            string args = string.Join(" ", commandArray);

            return new ProcessStartInfo()
            {
                FileName = exe,
                WorkingDirectory = _path,
                Arguments = args + " \"" + rom + "\"", 
            };
        }
        private void SetupPaths(string core)        
        {          
            string iniFile = Path.Combine(_path, "inis", "PCSX2_ui.ini");
            if (File.Exists(iniFile))
            {
                try
                {
                    using (var ini = new IniFile(iniFile))
                    {
                        Uri relRoot = new Uri(_path, UriKind.Absolute);

                        string biosPath = AppConfig.GetFullPath("bios");
                        if (!string.IsNullOrEmpty(biosPath))
                        {
                            ini.WriteValue("Folders", "UseDefaultBios", "disabled");
                            ini.WriteValue("Folders", "Bios", biosPath.Replace("\\", "\\\\"));
                        }

                        string savesPath = AppConfig.GetFullPath("saves");
                        if (!string.IsNullOrEmpty(savesPath))
                        {
                            savesPath = Path.Combine(savesPath, "ps2", "pcsx2");
                            if (!Directory.Exists(savesPath))
                                try { Directory.CreateDirectory(savesPath); }
                                catch { }
								
                            ini.WriteValue("Folders", "UseDefaultSavestates", "disabled");
							ini.WriteValue("Folders", "UseDefaultMemoryCards", "disabled");
                            ini.WriteValue("Folders", "Savestates", savesPath.Replace("\\", "\\\\")); // Path.Combine(relPath, "pcsx2")
							ini.WriteValue("Folders", "MemoryCards", savesPath.Replace("\\", "\\\\"));
                        }

                        if (SystemConfig.isOptSet("ratio") && !string.IsNullOrEmpty(SystemConfig["ratio"]))
                            ini.WriteValue("GSWindow", "AspectRatio", SystemConfig["ratio"]);
                        else
                            ini.WriteValue("GSWindow", "AspectRatio", "4:3");
						
						if (SystemConfig.isOptSet("fmv_ratio") && !string.IsNullOrEmpty(SystemConfig["fmv_ratio"]))
                            ini.WriteValue("GSWindow", "FMVAspectRatioSwitch", SystemConfig["fmv_ratio"]);
                        else
                            ini.WriteValue("GSWindow", "FMVAspectRatioSwitch", "Off");

                        ini.WriteValue("ProgramLog", "Visible", "disabled");
                        ini.WriteValue("GSWindow", "IsFullscreen", "enabled");

                        ini.WriteValue("Filenames", "PAD", "LilyPad.dll");

                        if (core == "pcsx2-avx2" || core == "avx2")
                        {
                            ini.WriteValue("Filenames", "GS", "GSdx32-AVX2.dll");
                        }
                        else if (core == "pcsx2-sse2" || core == "sse2")
                        {
                            ini.WriteValue("Filenames", "GS", "GSdx32-SSE2.dll");
                        }
                        else if (core == "pcsx2-sse4" || core == "sse4")
                        {
                            ini.WriteValue("Filenames", "GS", "GSdx32-SSE4.dll");
                        }
                        else
                            ini.WriteValue("Filenames", "GS", "GSdx32-AVX2.dll");
                    }
                }
                catch { }
            }
        }

        private void SetupLilyPad()
        {
            string iniFile = Path.Combine(_path, "inis", "LilyPad.ini");
            if (File.Exists(iniFile))
            {
                try
                {
                    using (var ini = new IniFile(iniFile))
                    {
                        ini.WriteValue("General Settings", "Keyboard Mode", "1");
                    }
                }
                catch { }
            }
        }

        private void SetupVM()
        {
            string iniFile = Path.Combine(_path, "inis", "PCSX2_vm.ini");
            if (File.Exists(iniFile))
            {
                try
                {
                    using (var ini = new IniFile(iniFile))
                    {
                        string negdivhack = SystemConfig["negdivhack"] == "1" ? "enabled" : "disabled";
						
						if (!string.IsNullOrEmpty(SystemConfig["VSync"]))
                            ini.WriteValue("EmuCore/GS", "VsyncEnable", SystemConfig["VSync"]);
                        else
                            ini.WriteValue("EmuCore/GS", "VsyncEnable", "0");

                        ini.WriteValue("EmuCore/Speedhacks", "vuThread", negdivhack);

                        ini.WriteValue("EmuCore/CPU/Recompiler", "vuExtraOverflow", negdivhack);
                        ini.WriteValue("EmuCore/CPU/Recompiler", "vuSignOverflow", negdivhack);
                        ini.WriteValue("EmuCore/CPU/Recompiler", "fpuExtraOverflow", negdivhack);
                        ini.WriteValue("EmuCore/CPU/Recompiler", "fpuFullMode", negdivhack);

                        ini.WriteValue("EmuCore/Gamefixes", "VuClipFlagHack", negdivhack);
                        ini.WriteValue("EmuCore/Gamefixes", "FpuNegDivHack", negdivhack);
                    }
                }
                catch { }
            }
        }
                        
        private void SetupGSDx(ScreenResolution resolution)
        {
            string iniFile = Path.Combine(_path, "inis", "GSdx.ini");
            if (File.Exists(iniFile))
            {
                try
                {
                    using (var ini = new IniFile(iniFile))
                    {
                        ini.WriteValue("Settings", "UserHacks", "1");

                        if (!string.IsNullOrEmpty(SystemConfig["internalresolution"]))
                            ini.WriteValue("Settings", "upscale_multiplier", SystemConfig["internalresolution"]);
                        else
                            ini.WriteValue("Settings", "upscale_multiplier", "1");

                        if (string.IsNullOrEmpty(SystemConfig["internalresolution"]) || SystemConfig["internalresolution"] == "0")
                        {
                            if (resolution != null)
                            {
                                ini.WriteValue("Settings", "resx", resolution.Width.ToString());
                                ini.WriteValue("Settings", "resy", (resolution.Height * 2).ToString());
                            }
                            else
                            {
                                ini.WriteValue("Settings", "resx", Screen.PrimaryScreen.Bounds.Width.ToString());
                                ini.WriteValue("Settings", "resy", (Screen.PrimaryScreen.Bounds.Height * 2).ToString());
                            }
                        }

                        ini.WriteValue("Settings", "shaderfx", "1");

                        if (SystemConfig.isOptSet("TVShader") && !string.IsNullOrEmpty(SystemConfig["TVShader"]))
                            ini.WriteValue("Settings", "TVShader", SystemConfig["TVShader"]);
                        else
                            ini.WriteValue("Settings", "TVShader", "0");

                        if (SystemConfig.isOptSet("Offset") && !string.IsNullOrEmpty(SystemConfig["Offset"]))
                            ini.WriteValue("Settings", "UserHacks_WildHack", SystemConfig["Offset"]);
                        else
                            ini.WriteValue("Settings", "UserHacks_WildHack", "0");

                        if (SystemConfig.isOptSet("bilinear_filtering") && !string.IsNullOrEmpty(SystemConfig["bilinear_filtering"]))
                            ini.WriteValue("Settings", "linear_present", SystemConfig["bilinear_filtering"]);
                        else
                            ini.WriteValue("Settings", "linear_present", "0");

                        if (SystemConfig.isOptSet("fxaa") && !string.IsNullOrEmpty(SystemConfig["fxaa"]))
                            ini.WriteValue("Settings", "fxaa", SystemConfig["fxaa"]);
                        else
                            ini.WriteValue("Settings", "fxaa", "0");

                        if (SystemConfig.isOptSet("renderer") && !string.IsNullOrEmpty(SystemConfig["renderer"]))
                            ini.WriteValue("Settings", "Renderer", SystemConfig["renderer"]);
                        else
                            ini.WriteValue("Settings", "Renderer", "12");

                        if (SystemConfig.isOptSet("interlace") && !string.IsNullOrEmpty(SystemConfig["interlace"]))
                            ini.WriteValue("Settings", "interlace", SystemConfig["interlace"]);
                        else
                            ini.WriteValue("Settings", "interlace", "7");

                        if (SystemConfig.isOptSet("anisotropic_filtering") && !string.IsNullOrEmpty(SystemConfig["anisotropic_filtering"]))
                            ini.WriteValue("Settings", "MaxAnisotropy", SystemConfig["anisotropic_filtering"]);
                        else
                            ini.WriteValue("Settings", "MaxAnisotropy", "0");

                        if (SystemConfig.isOptSet("align_sprite") && !string.IsNullOrEmpty(SystemConfig["align_sprite"]))
                            ini.WriteValue("Settings", "UserHacks_align_sprite_X", SystemConfig["align_sprite"]);
                        else
                            ini.WriteValue("Settings", "UserHacks_align_sprite_X", "0");

                        if (SystemConfig.isOptSet("skipdraw") && !string.IsNullOrEmpty(SystemConfig["skipdraw"]))
                            ini.WriteValue("Settings", "UserHacks_SkipDraw", SystemConfig["skipdraw"]);
                        else
                            ini.WriteValue("Settings", "UserHacks_SkipDraw", "0");

                        if (SystemConfig.isOptSet("DrawFramerate") && SystemConfig.getOptBoolean("DrawFramerate"))
                        {
                            ini.WriteValue("Settings", "osd_monitor_enabled", "1");
                            ini.WriteValue("Settings", "osd_indicator_enabled", "1");
                        }
                        else
                        {
                            ini.WriteValue("Settings", "osd_monitor_enabled", "0");
                            ini.WriteValue("Settings", "osd_indicator_enabled", "0");
                        }
                        
                    }

                }
                catch { }
            }
        }

        /*
        private string romName;
        private const string savDirName = "tmp";
        
        public override void Cleanup()
        {
            RestoreIni(path, romName, "GSdx.ini");
            RestoreIni(path, romName, "PCSX2_vm.ini");

            try
            {
                string savDir = Path.Combine(path, "inis", savDirName);
                if (Directory.Exists(savDir))
                    Directory.Delete(savDir);
            }
            catch { }
        }
     
        static void SaveIni(string path, string romName, string iniName)
        {
            string ini = Path.Combine(path, "inis", romName, iniName);
            if (!File.Exists(ini))
                return;

            string originalIni = Path.Combine(path, "inis", iniName);
            if (File.Exists(originalIni))
            {
                string savDir = Path.Combine(path, "inis", savDirName);
                if (!Directory.Exists(savDir))
                    Directory.CreateDirectory(savDir);

                string savIni = Path.Combine(path, "inis", savDirName, iniName);

                try { File.Copy(originalIni, savIni, true); }
                catch { return; }
            }

            try { File.Copy(ini, originalIni, true); }
            catch { }

        }

        static void RestoreIni(string path, string romName, string iniName, bool force = false)
        {
            if (string.IsNullOrEmpty(romName))
                return;

            if (!force)
            {
                string ini = Path.Combine(path, "inis", romName, iniName);
                if (!File.Exists(ini))
                    return;
            }

            string originalIni = Path.Combine(path, "inis", iniName);
            if (File.Exists(originalIni))
            {
                string savIni = Path.Combine(path, "inis", savDirName, iniName);
                if (File.Exists(savIni))
                {
                    try { File.Move(savIni, originalIni); }
                    catch { }

                    try { File.Delete(savIni); }
                    catch { }

                }
            }
        }   */
    }
}
