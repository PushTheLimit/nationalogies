using System;
using System.IO;
using System.Reflection;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Castle.Windsor;
using Cuyahoga.Core.Util;
using Cuyahoga.Web.Components;
using Cuyahoga.Web.Mvc.Areas;
using log4net;
using log4net.Config;

namespace Cuyahoga.Web
{
	public class Global : HttpApplication, IContainerAccessor
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(Global));
		private static readonly string ERROR_PAGE_LOCATION = "~/Error.aspx";
		private static readonly AspNetHostingPermissionLevel TrustLevel = GetCurrentTrustLevel();

		/// <summary>
		/// Obtain the container.
		/// </summary>
		public IWindsorContainer Container
		{
			get { return IoC.Container; }
		}

		/// <summary>
		/// Gets the current trust level.
		/// </summary>
		public static AspNetHostingPermissionLevel CurrentTrustLevel
		{
			get { return TrustLevel; }
		}

		protected void Application_Start(Object sender, EventArgs e)
		{
			// Initialize log4net 
			XmlConfigurator.ConfigureAndWatch(new FileInfo(Server.MapPath("~/") + "config/logging.config"));

			// Set application level flags.
			// TODO: get rid of application variables.
			HttpContext.Current.Application.Lock();
			HttpContext.Current.Application["IsFirstRequest"] = true;
			HttpContext.Current.Application["ModulesLoaded"] = false;
			HttpContext.Current.Application["IsModuleLoading"] = false;
			HttpContext.Current.Application["IsInstalling"] = false;
			HttpContext.Current.Application["IsUpgrading"] = false;
			HttpContext.Current.Application.UnLock();

			// Initialize container
			Bootstrapper.InitializeContainer();

			// View engine
			ViewEngines.Engines.Clear();
			ViewEngines.Engines.Add(new CuyahogaAreaViewEngine());

			// Routes
			RegisterRoutes(RouteTable.Routes);
			//RouteHelper.RouteDebugger.RewriteRoutesForTesting(RouteTable.Routes);

			// Misc
			FixAppDomainRestartWhenTouchingFiles();
		}

		protected void Application_BeginRequest(object sender, EventArgs e)
		{
			// Bootstrap Cuyahoga at the first request. We can't do this in Application_Start because
			// we need the HttpContext.Response object to perform redirect. In IIS 7 integrated mode, the 
			// Response isn't available in Application_Start.
			if ((bool)HttpContext.Current.Application["IsFirstRequest"])
			{
				Bootstrapper.InitializeCuyahoga();
				HttpContext.Current.Application.Lock();
				HttpContext.Current.Application["IsFirstRequest"] = false;
				HttpContext.Current.Application.UnLock();
			}

			// Load active modules. This can't be done in Application_Start because the Installer might kick in
			// before modules are loaded.
			if (!(bool)HttpContext.Current.Application["ModulesLoaded"]
				&& !(bool)HttpContext.Current.Application["IsModuleLoading"]
				&& !(bool)HttpContext.Current.Application["IsInstalling"]
				&& !(bool)HttpContext.Current.Application["IsUpgrading"])
			{
				LoadModules();
			}
		}

		protected void Application_Error(object sender, EventArgs e)
		{
			if (Context != null && Context.IsCustomErrorEnabled)
			{
				Server.Transfer(ERROR_PAGE_LOCATION, false);
			}
		}

		protected void Application_End(object sender, EventArgs e)
		{
			Container.Dispose();
		}

		public static void RegisterRoutes(RouteCollection routes)
		{
			routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
			routes.IgnoreRoute("{*allaspx}", new { allaspx = @".*\.aspx(/.*)?" });
			routes.IgnoreRoute("{*allashx}", new { allashx = @".*\.ashx(/.*)?" });
			routes.IgnoreRoute("{*favicon}", new { favicon = @"(.*/)?favicon.ico(/.*)?" });

			routes.CreateArea("manager", "Cuyahoga.Web.Manager.Controllers",
				routes.MapRoute(null, "manager/Pages/Design/{id}/Section/{sectionId}", new { action = "Design", controller = "Pages" }),
				routes.MapRoute(null, "manager/{controller}/{action}/{id}", new { action = "Index", controller = "Dashboard", id = "" }),
				routes.MapRoute(null, "Login", new { action = "Index", controller = "Login" }) // Also put the login functionality in the manager area.
			);
			routes.CreateArea("modules/shared", "Cuyahoga.Web.Modules.Shared.Controllers",
				routes.MapRoute(null, "modules/shared/{controller}/{action}/{id}", new { action = "Index", id = "" })
			);

			// Routing config for the root area (not currently used)
			//routes.CreateArea("root", "Cuyahoga.Web.Controllers",
			//    routes.MapRoute(null, "{controller}/{action}", new { controller = "Home", action = "Index" })
			//);
		}

		private void LoadModules()
		{
			if (log.IsDebugEnabled)
			{
				log.Debug("Entering module loading.");
			}
			// Load module types into the container.
			ModuleLoader loader = Container.Resolve<ModuleLoader>();
			loader.RegisterActivatedModules();

			if (log.IsDebugEnabled)
			{
				log.Debug("Finished module loading. Now redirecting to self.");
			}
			// Re-load the requested page (to avoid conflicts with first-time configured NHibernate modules )
			HttpContext.Current.Response.Redirect(HttpContext.Current.Request.RawUrl);

		}

		private void FixAppDomainRestartWhenTouchingFiles()
		{
			if (CurrentTrustLevel == AspNetHostingPermissionLevel.Unrestricted)
			{
				// From: http://forums.asp.net/p/1310976/2581558.aspx
				// FIX disable AppDomain restart when deleting subdirectory
				// This code will turn off monitoring from the root website directory.
				// Monitoring of Bin, App_Themes and other folders will still be operational, so updated DLLs will still auto deploy.
				PropertyInfo p = typeof(HttpRuntime).GetProperty("FileChangesMonitor", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
				object o = p.GetValue(null, null);
				FieldInfo f = o.GetType().GetField("_dirMonSubdirs", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.IgnoreCase);
				object monitor = f.GetValue(o);
				MethodInfo m = monitor.GetType().GetMethod("StopMonitoring", BindingFlags.Instance | BindingFlags.NonPublic); 
				m.Invoke(monitor, new object[] { });
			}
		}

		private static AspNetHostingPermissionLevel GetCurrentTrustLevel()
		{
			foreach (AspNetHostingPermissionLevel trustLevel in
					new AspNetHostingPermissionLevel[] {
                AspNetHostingPermissionLevel.Unrestricted,
                AspNetHostingPermissionLevel.High,
                AspNetHostingPermissionLevel.Medium,
                AspNetHostingPermissionLevel.Low,
                AspNetHostingPermissionLevel.Minimal 
            })
			{
				try
				{
					new AspNetHostingPermission(trustLevel).Demand();
				}
				catch (System.Security.SecurityException)
				{
					continue;
				}

				return trustLevel;
			}

			return AspNetHostingPermissionLevel.None;
		}
	}
}



