
// Copyright (C) 2015-2017 Luca Piccioni
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.IO;

using Khronos;

using BindingsGen.GLSpecs;
using CommandLine;

namespace BindingsGen {
	/// <summary>
	/// OpenGL bindings generator.
	/// </summary>
	internal class Program {
		enum GlProfiles {
			Core,
			ES2,
			SC2
		}
		enum GenType {
			Gl,
			Wgl,
			Glx,
			Egl
		}

		class Options {
			[Option('d', "dummy", Required = false, HelpText = "Set output to generate dummies.")]
			public bool DummyStream { get; set; }
		}

		[Verb("logmaps")]
		class LogMapOptions {
			[Value(0)]
			public GenType Gen { get; set; }
			/// <summary>
			/// Base path to construct correct file paths.
			/// </summary>
			[Option('i', "input", HelpText = "Base path to construct correct file paths.", Required = true)]
			public string BasePath { get; set; } = "../../../..";
		}

		[Verb("assembly")]
		class AssemblyOptions {
			[Value(0)]
			public string? AssemblyPath { get; set; }

			[Option("overwrite", Required = false, HelpText = "Overwrite assembly.")]
			public bool AssemblyOverwrite { get; set; }

			[Option("profile", Required = false, HelpText = "GL profile.")]
			public GlProfiles[]? GlProfiles { get; set; }
		}
		[Verb("gen")]
		class GenOptions {

			[Option("nodoc", Required = false, HelpText = "Set output to not generate documentation.")]
			public bool DocDisabled { get; set; }
			
			[Value(0)]
			public GenType GenType { get; set; }
			
			[Option('c', "commands", Group = "gen")]
			public bool Commands { get; set; }
			[Option('e', "extensions", Group = "gen")]
			public bool Extensions { get; set; }
			[Option('l', "limits", Group = "gen")]
			public bool Limits { get; set; }
			
			/// <summary>
			/// Base path to construct correct file paths.
			/// </summary>
			[Option('i', "input", HelpText = "Base path to construct correct file paths.", Required = true)]
			public string BasePath { get; set; }

			/// <summary>
			/// The directory where the output files are placed.
			/// </summary>
			[Option('o', "output", HelpText = "The directory where the output files are placed.", Required = true)]
			public string OutputBasePath { get; set; }
		}
		
		public static bool DummyStream { get; set; }
		public static string BasePath { get; set; }
		public static string OutputBasePath { get; set; }
		

		/// <summary>
		/// Application entry point.
		/// </summary>
		/// <param name="args">
		/// The command line invocation arguments.
		/// </param>
		private static void Main(string[] args) {
			RegistryContext ctx;
			RegistryProcessor glRegistryProcessor;
			int index;
			Parser.Default.ParseArguments<Options, LogMapOptions, AssemblyOptions, GenOptions>(args)
				.WithParsed<Options>(o => DummyStream = o.DummyStream)
				#region Assembly processing

				.WithParsed<AssemblyOptions>(o => {
					var cfgs = new List<RegistryAssemblyConfiguration>();

					if (o.GlProfiles is null) {
						cfgs.Add(RegistryAssemblyConfiguration.Load("BindingsGen.Profiles.CoreProfile.xml"));
						cfgs.Add(RegistryAssemblyConfiguration.Load("BindingsGen.Profiles.ES2Profile.xml"));
						cfgs.Add(RegistryAssemblyConfiguration.Load("BindingsGen.Profiles.SC2Profile.xml"));
					}
					else {
						if (Array.Exists(o.GlProfiles, item => item == GlProfiles.Core))
							cfgs.Add(RegistryAssemblyConfiguration.Load("BindingsGen.Profiles.CoreProfile.xml"));
						if (Array.Exists(o.GlProfiles, item => item == GlProfiles.ES2))
							cfgs.Add(RegistryAssemblyConfiguration.Load("BindingsGen.Profiles.ES2Profile.xml"));
						if (Array.Exists(o.GlProfiles, item => item == GlProfiles.SC2))
							cfgs.Add(RegistryAssemblyConfiguration.Load("BindingsGen.Profiles.SC2Profile.xml"));
					}

					foreach (var cfg in cfgs) {
						try {
							RegistryAssembly.CleanAssembly(o.AssemblyPath, cfg, o.AssemblyOverwrite);
						}
						catch (Exception exception) {
							Console.Error.WriteLine(exception);
						}
					}
				})

				#endregion

				#region Log Maps

				.WithParsed<LogMapOptions>(o => {
					Console.WriteLine("Generating " + o.Gen + " log map...");
					ctx = new RegistryContext("" + o.Gen, "" + o.Gen,
						Path.Combine(o.BasePath, "GLSpecs/" + o.Gen.ToString().ToLower()));
					glRegistryProcessor = new RegistryProcessor(ctx.Registry);
					glRegistryProcessor.GenerateLogMap(ctx,
						Path.Combine(o.BasePath, "OpenGL.Net/KhronosLogMap" + o.Gen + ".xml"));
				})

				#endregion

				.WithParsed<GenOptions>(o => {
					BasePath = o.BasePath;
					OutputBasePath = o.OutputBasePath;
					// (Common) Documentation
					var gl4Documentation = new RegistryDocumentation<RegistryDocumentationHandlerGL4>();
					var gl2Documentation = new RegistryDocumentation<RegistryDocumentationHandlerGL2>();

					if (o.GenType is GenType.Gl or GenType.Egl or GenType.Glx) {
						gl4Documentation.Api = "GL4";
						if (o.DocDisabled)
							gl4Documentation.ScanDocumentation(Path.Combine(o.BasePath, "RefPages/OpenGL/gl4"));

						gl2Documentation.Api = "GL2.1";
						if (o.DocDisabled)
							gl2Documentation.ScanDocumentation(Path.Combine(o.BasePath, "RefPages/OpenGL/gl2.1"));
					}

					// XML-based specifications

					switch (o.GenType) {
						// OpenGL
						case GenType.Gl: {
							// Additional ES documentation
							var gles3Documentation =
								new RegistryDocumentation<RegistryDocumentationHandlerGL4> { Api = "GLES3.2" };
							if (!o.DocDisabled)
								gles3Documentation.ScanDocumentation(Path.Combine(o.BasePath, "RefPages/OpenGL/es3"));

							var gles1Documentation =
								new RegistryDocumentation<RegistryDocumentationHandlerGL2> { Api = "GLES1.1" };
							if (!o.DocDisabled)
								gles1Documentation.ScanDocumentation(Path.Combine(o.BasePath, "RefPages/OpenGL/es1.1"));

							Console.WriteLine("Loading GL specification...");
							ctx = new RegistryContext("Gl", "Gl", Path.Combine(o.BasePath, "GLSpecs/gl.xml"));
							ctx.RefPages.Add(gl4Documentation);
							ctx.RefPages.Add(gl2Documentation);
							ctx.RefPages.Add(gles3Documentation);
							ctx.RefPages.Add(gles1Documentation);

							glRegistryProcessor = new RegistryProcessor(ctx.Registry);
							glRegistryProcessor.GenerateStronglyTypedEnums(ctx,
								Path.Combine(o.BasePath, $"{o.OutputBasePath}/{ctx.Class}.Enums.cs"));
							if (o.Commands)
								glRegistryProcessor.GenerateCommandsAndEnums(ctx);
							if (o.Extensions)
								glRegistryProcessor.GenerateExtensionsSupportClass(ctx);
							if (o.Limits)
								glRegistryProcessor.GenerateLimitsSupportClass(ctx);
							glRegistryProcessor.GenerateVersionsSupportClass(ctx);
							glRegistryProcessor.GenerateVbCommands(ctx);
							glRegistryProcessor.GenerateLogMap(ctx,
								Path.Combine(o.BasePath, "OpenGL.Net/KhronosLogMapGl.xml"));
							break;
						}
						// OpenGL for Windows
						case GenType.Wgl:
							ctx = new RegistryContext("Wgl", "Wgl", Path.Combine(o.BasePath, "GLSpecs/wgl.xml"));
							ctx.RefPages.Add(gl4Documentation);
							ctx.RefPages.Add(gl2Documentation);

							glRegistryProcessor = new RegistryProcessor(ctx.Registry);
							glRegistryProcessor.GenerateStronglyTypedEnums(ctx,
								Path.Combine(o.BasePath, $"{o.OutputBasePath}/{ctx.Class}.Enums.cs"));
							glRegistryProcessor.GenerateCommandsAndEnums(ctx);
							glRegistryProcessor.GenerateExtensionsSupportClass(ctx);
							glRegistryProcessor.GenerateVersionsSupportClass(ctx);
							glRegistryProcessor.GenerateVbCommands(ctx);
							glRegistryProcessor.GenerateLogMap(ctx,
								Path.Combine(o.BasePath, "OpenGL.Net/KhronosLogMapWgl.xml"));
							break;
						// OpenGL for Unix
						case GenType.Glx:
							ctx = new RegistryContext("Glx", "Glx", Path.Combine(o.BasePath, "GLSpecs/glx.xml"));
							ctx.RefPages.Add(gl4Documentation);
							ctx.RefPages.Add(gl2Documentation);

							glRegistryProcessor = new RegistryProcessor(ctx.Registry);
							glRegistryProcessor.GenerateStronglyTypedEnums(ctx,
								Path.Combine(o.BasePath, $"{o.OutputBasePath}/{ctx.Class}.Enums.cs"));
							glRegistryProcessor.GenerateCommandsAndEnums(ctx);
							glRegistryProcessor.GenerateExtensionsSupportClass(ctx);
							glRegistryProcessor.GenerateVersionsSupportClass(ctx);
							glRegistryProcessor.GenerateVbCommands(ctx);
							glRegistryProcessor.GenerateLogMap(ctx,
								Path.Combine(o.BasePath, "OpenGL.Net/KhronosLogMapGlx.xml"));
							break;
						// EGL
						case GenType.Egl: {
							var eglDocumentation =
								new RegistryDocumentation<RegistryDocumentationHandlerEGL> { Api = "EGL" };
							eglDocumentation.ScanDocumentation(Path.Combine(o.BasePath,
								"RefPages/EGL-Registry/sdk/docs/man"));

							ctx = new RegistryContext("Egl", "Egl", Path.Combine(o.BasePath, "GLSpecs/egl.xml"));
							ctx.RefPages.Add(eglDocumentation);

							glRegistryProcessor = new RegistryProcessor(ctx.Registry);
							glRegistryProcessor.GenerateStronglyTypedEnums(ctx,
								Path.Combine(o.BasePath, $"{o.OutputBasePath}/{ctx.Class}.Enums.cs"));
							glRegistryProcessor.GenerateCommandsAndEnums(ctx);
							glRegistryProcessor.GenerateExtensionsSupportClass(ctx);
							glRegistryProcessor.GenerateVersionsSupportClass(ctx);
							glRegistryProcessor.GenerateVbCommands(ctx);
							glRegistryProcessor.GenerateLogMap(ctx,
								Path.Combine(o.BasePath, "OpenGL.Net/KhronosLogMapEgl.xml"));
							break;
						}
					}
				});
		}

#if SUPPORT_OPENWF

			// OpenWF

			OutputBasePath = "OpenWF.Net";

			// OpenWF(C)
			// Note: you must setup CLI to generate this bindings
			if ((args.Length > 0) && (Array.FindIndex(args, item => item == "--wfc") >= 0)) {
				Header headRegistry = new Header("Wfc")
				{
					CommandExportRegex = "WF(D|C)_APIENTRY ",
					CommandCallConventionRegex = "WF(D|C)_API_CALL ",
					CommandExitRegex = " WF(D|C)_APIEXIT"
				};
				headRegistry.AppendHeader(Path.Combine(BasePath, "GLSpecs/WF/wfc.h"), new KhronosVersion(1, 0, KhronosVersion.ApiWfc));

				ctx = new RegistryContext("Wfc", "Wfc", headRegistry);
				glRegistryProcessor = new RegistryProcessor(ctx.Registry, "OpenWF");
				glRegistryProcessor.GenerateStronglyTypedEnums(ctx, Path.Combine(BasePath, $"{OutputBasePath}/{ctx.Class}.Enums.cs"));
				glRegistryProcessor.GenerateCommandsAndEnums(ctx);
				glRegistryProcessor.GenerateExtensionsSupportClass(ctx);
				glRegistryProcessor.GenerateVersionsSupportClass( ctx);
				glRegistryProcessor.GenerateVbCommands(ctx);
			}

			// OpenWF(D)
			if ((args.Length == 0) || (Array.FindIndex(args, item => item == "--wfd") >= 0)) {
				Header headRegistry = new Header("Wfd")
				{
					CommandExportRegex = "WF(D|C)_APIENTRY ",
					CommandCallConventionRegex = "WF(D|C)_API_CALL ",
					CommandExitRegex = " WF(D|C)_APIEXIT"
				};
				headRegistry.AppendHeader(Path.Combine(BasePath, "GLSpecs/WF/wfd.h"), new KhronosVersion(1, 0, KhronosVersion.ApiWfd));

				ctx = new RegistryContext("Wfd", "Wfd", headRegistry);
				glRegistryProcessor = new RegistryProcessor(ctx.Registry, "OpenWF");
				glRegistryProcessor.GenerateStronglyTypedEnums(ctx, Path.Combine(BasePath, $"{OutputBasePath}/{ctx.Class}.Enums.cs"));
				glRegistryProcessor.GenerateCommandsAndEnums(ctx);
				glRegistryProcessor.GenerateExtensionsSupportClass(ctx);
				glRegistryProcessor.GenerateVersionsSupportClass(ctx);
				glRegistryProcessor.GenerateVbCommands(ctx);
			}

#endif

#if SUPPORT_OPENVX

			// OpenVX

			OutputBasePath = "OpenVX.Net";

			if ((args.Length == 0) || (Array.FindIndex(args, item => item == "--vx") >= 0)) {
				Header vxRegistry = new Header("VX")
				{
					CommandExportRegex = "VX_API_ENTRY ",
					CommandCallConventionRegex = "VX_API_CALL "
				};
				vxRegistry.AppendHeader(Path.Combine(BasePath, "VXSpecs/1.2/vx_api.h"), new KhronosVersion(1, 2, KhronosVersion.ApiVx));
				vxRegistry.AppendHeader(Path.Combine(BasePath, "VXSpecs/1.2/vx_types.h"), new KhronosVersion(1, 2, KhronosVersion.ApiVx));
				vxRegistry.AppendHeader(Path.Combine(BasePath, "VXSpecs/1.2/vx_kernels.h"), new KhronosVersion(1, 2, KhronosVersion.ApiVx));
				vxRegistry.AppendHeader(Path.Combine(BasePath, "VXSpecs/1.2/vx_nodes.h"), new KhronosVersion(1, 2, KhronosVersion.ApiVx));
				vxRegistry.AppendHeader(Path.Combine(BasePath, "VXSpecs/1.2/vx_vendors.h"), new KhronosVersion(1, 2, KhronosVersion.ApiVx));
				vxRegistry.AppendHeader(Path.Combine(BasePath, "VXSpecs/1.2/vx_import.h"), new KhronosVersion(1, 2, KhronosVersion.ApiVx));

				vxRegistry.AppendHeader(Path.Combine(BasePath, "VXSpecs/1.2/vx_khr_icd.h"), "VX_KHR_icd");
				vxRegistry.AppendHeader(Path.Combine(BasePath, "VXSpecs/1.2/vx_khr_ix.h"), "VX_KHR_ix");
				vxRegistry.AppendHeader(Path.Combine(BasePath, "VXSpecs/1.2/vx_khr_nn.h"), "VX_KHR_nn");
				vxRegistry.AppendHeader(Path.Combine(BasePath, "VXSpecs/1.2/vx_khr_tiling.h"), "VX_KHR_tiling");
				vxRegistry.AppendHeader(Path.Combine(BasePath, "VXSpecs/1.2/vx_khr_xml.h"), "VX_KHR_xml");

				ctx = new RegistryContext("VX", "VX", vxRegistry);
				glRegistryProcessor = new RegistryProcessor(ctx.Registry, "OpenVX");
				glRegistryProcessor.GenerateStronglyTypedEnums(ctx, Path.Combine(BasePath, $"{OutputBasePath}/{ctx.Class}.Enums.cs"));
				glRegistryProcessor.GenerateCommandsAndEnums(ctx);
				//glRegistryProcessor.GenerateExtensionsSupportClass(ctx);
				//glRegistryProcessor.GenerateVersionsSupportClass(ctx);

				Header vxuRegistry = new Header("VXU")
				{
					CommandExportRegex = "VX_API_ENTRY ",
					CommandCallConventionRegex = "VX_API_CALL "
				};
				vxuRegistry.AppendHeader(Path.Combine(BasePath, "VXSpecs/1.2/vxu.h"), new KhronosVersion(1, 2, KhronosVersion.ApiVx));

				ctx = new RegistryContext("VXU", "VX", vxuRegistry);
				glRegistryProcessor = new RegistryProcessor(ctx.Registry, "OpenVX");
				glRegistryProcessor.GenerateCommandsAndEnums(ctx);
			}

#endif
		}

	}
