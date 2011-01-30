﻿using System;
using System.Collections;
using System.IO;
using System.Web;
using System.Web.Mvc;
using Cuyahoga.Core.Communication;
using Cuyahoga.Core.Domain;
using Cuyahoga.Core.Service.Files;
using Cuyahoga.Core.Service.Membership;
using Cuyahoga.Core.Service.SiteStructure;
using Cuyahoga.Core.Util;
using Cuyahoga.Core.Validation;
using Cuyahoga.Web.Components;
using Cuyahoga.Web.Manager.Filters;
using Cuyahoga.Web.Manager.Helpers;
using Cuyahoga.Web.Manager.Model.ViewModels;
using Cuyahoga.Web.Mvc.Filters;
using Cuyahoga.Web.Mvc.WebForms;

namespace Cuyahoga.Web.Manager.Controllers
{
	[PermissionFilter(RequiredRights = Rights.ManagePages)]
	public class PagesController : ManagerController
	{
		private readonly INodeService _nodeService;
		private readonly ITemplateService _templateService;
		private readonly IFileService _fileService;
		private readonly ISectionService _sectionService;
		private ModuleLoader _moduleLoader;

		public PagesController(INodeService nodeService, ITemplateService templateService, IFileService fileService, ISectionService sectionService, IModelValidator<Node> modelValidator, ModuleLoader moduleLoader)
		{
			_nodeService = nodeService;
			_templateService = templateService;
			_fileService = fileService;
			_sectionService = sectionService;
			this.ModelValidator = modelValidator;
			_moduleLoader = moduleLoader;
		}

		[RolesFilter]
		public ActionResult Index(int? id)
		{
			// The given id is of the active node.
			if (id.HasValue)
			{
				// There is an active node. Add node to ViewData.
				Node activeNode = this._nodeService.GetNodeById(id.Value);
				if (activeNode.IsExternalLink)
				{
					ViewData["LinkTargets"] = new SelectList(EnumHelper.GetValuesWithDescription<LinkTarget>(), "Key", "Value", (int)activeNode.LinkTarget);
				}
				else
				{
					ViewData["Cultures"] = new SelectList(Globalization.GetOrderedCultures(), "Key", "Value", activeNode.Culture);
				}
				ViewData["ActiveNode"] = activeNode;
				ViewData["NewLinkTargets"] = new SelectList(EnumHelper.GetValuesWithDescription<LinkTarget>(), "Key", "Value");
			}
			ViewData["AvailableCultures"] = GetAvailableCultures();
			return View("Index", CuyahogaContext.CurrentSite.RootNodes);
		}

		[RolesFilter]
		public ActionResult Design(int id, int? sectionId, bool? expandAddNew)
		{
			Node node = this._nodeService.GetNodeById(id);
			if (sectionId.HasValue)
			{
				ViewData["ActiveSection"] = BuildSectionViewData(this._sectionService.GetSectionById(sectionId.Value));
			}
			ViewData["Templates"] = new SelectList(this._templateService.GetAllTemplatesBySite(CuyahogaContext.CurrentSite), "Id", "Name"
				, node.Template != null ? node.Template.Id : -1);
			ViewData["AvailableModules"] = this._sectionService.GetSortedActiveModuleTypes();
			if (node.Template != null)
			{
				ViewData["TemplateViewData"] = BuildTemplateViewData(node.Template);
			}
			if (expandAddNew.HasValue)
			{
				ViewData["ExpandAddNew"] = expandAddNew.Value;
			}
			return View(node);
		}

		public ActionResult Content(int id)
		{
			Node node = this._nodeService.GetNodeById(id);
			return View(node);
		}

		[AcceptVerbs(HttpVerbs.Post)]
		public ActionResult SavePageProperties(int id)
		{
			Node node = this._nodeService.GetNodeById(id);
			var includeProperties = new[] { "Title", "ShortDescription", "Culture", "ShowInNavigation" };
			try
			{
				if (TryUpdateModel(node, includeProperties) && ValidateModel(node, includeProperties))
				{
					this._nodeService.SaveNode(node);
					Messages.AddFlashMessage("PagePropertiesUpdatedMessage");
					return RedirectToAction("Index", new { id = node.Id });
				}
			}
			catch (Exception ex)
			{
				Messages.AddException(ex);
			}
			return Index(node.Id);
		}

		[AcceptVerbs(HttpVerbs.Post)]
		public ActionResult SaveLinkProperties(int id)
		{
			Node node = this._nodeService.GetNodeById(id);
			var includeProperties = new[] { "Title", "LinkUrl", "LinkTarget" };
			try
			{
				if (TryUpdateModel(node, includeProperties) && ValidateModel(node, includeProperties))
				{
					this._nodeService.SaveNode(node);
					Messages.AddFlashMessage("LinkPropertiesUpdatedMessage");
					return RedirectToAction("Index", new { id = node.Id });
				}
			}
			catch (Exception ex)
			{
				Messages.AddException(ex);
			}
			return Index(node.Id);
		}

		[AcceptVerbs(HttpVerbs.Post)]
		public ActionResult MovePage(int nodeId, int newParentNodeId)
		{
			this._nodeService.MoveNode(nodeId, newParentNodeId);
			Messages.AddFlashMessage("PageMovedMessage");
			return RedirectToAction("Index", new { id = nodeId });
		}

		[AcceptVerbs(HttpVerbs.Post)]
		public ActionResult CopyPage(int nodeId, int newParentNodeId)
		{
			Node newNode = this._nodeService.CopyNode(nodeId, newParentNodeId);
			Messages.AddFlashMessage("PageCopiedMessage");
			return RedirectToAction("Index", new { id = newNode.Id });
		}

		[AcceptVerbs(HttpVerbs.Post)]
		public ActionResult CreateRootPage([Bind(Include = "Title, Culture")]Node newRootPage)
		{
			if (ValidateModel(newRootPage, new[] { "Title", "Culture" }, "NewRootPage" ))
			{
				try
				{
					newRootPage = this._nodeService.CreateRootNode(CuyahogaContext.CurrentSite, newRootPage);
					Messages.AddFlashMessageWithParams("PageCreatedMessage", newRootPage.Title);
					return RedirectToAction("Design", new { id = newRootPage.Id, expandaddnew = true });
				}
				catch (Exception ex)
				{
					Messages.AddException(ex);
				}
			}
			ViewData["CurrentTask"] = "CreateRootPage";
			return Index(null);
		}

		[AcceptVerbs(HttpVerbs.Post)]
		public ActionResult CreatePage(int parentNodeId, [Bind(Include = "Title")]Node newPage)
		{
			if (ValidateModel(newPage, new[] { "Title" }, "NewPage" ))
			{
				try
				{
					Node parentNode = this._nodeService.GetNodeById(parentNodeId);
					newPage = this._nodeService.CreateNode(parentNode, newPage);
					Messages.AddFlashMessageWithParams("PageCreatedMessage", newPage.Title);
					return RedirectToAction("Design", new { id = newPage.Id, expandaddnew = true });
				}
				catch (Exception ex)
				{
					Messages.AddException(ex);
				}
			}
			ViewData["CurrentTask"] = "CreatePage";
			return Index(parentNodeId);
		}

		[AcceptVerbs(HttpVerbs.Post)]
		public ActionResult CreateLink(int parentNodeId, [Bind(Include = "Title, LinkUrl, LinkTarget")]Node newLink)
		{
			if (ValidateModel(newLink, new[] { "Title", "LinkUrl" }, "NewLink" ))
			{
				try
				{
					Node parentNode = this._nodeService.GetNodeById(parentNodeId);
					newLink = this._nodeService.CreateNode(parentNode, newLink);
					Messages.AddFlashMessageWithParams("LinkCreatedMessage", newLink.Title);
					return RedirectToAction("Index", new { id = newLink.Id });
				}
				catch (Exception ex)
				{
					Messages.AddException(ex);
				}
			}
			ViewData["CurrentTask"] = "CreateLink";
			return Index(parentNodeId);
		}

		[AcceptVerbs(HttpVerbs.Post)]
		public ActionResult Delete(int id)
		{
			Node nodeToDelete = this._nodeService.GetNodeById(id);
			Node parentNode = nodeToDelete.ParentNode;
			try
			{
				string message = "PageDeletedMessage";
				if (nodeToDelete.IsExternalLink)
				{
					message = "LinkDeletedMessage";
				}
				this._nodeService.DeleteNode(nodeToDelete);
				Messages.AddFlashMessageWithParams(message, nodeToDelete.Title);
				if (parentNode != null)
				{
					return RedirectToAction("Index", new { id = parentNode.Id });
				}
				return RedirectToAction("Index");
			}
			catch (Exception ex)
			{
				Messages.AddFlashException(ex);
			}
			return RedirectToAction("Index", new { id = nodeToDelete.Id });
		}

		[AcceptVerbs(HttpVerbs.Post)]
		public ActionResult SetPagePermissions(int id, int[] viewRoleIds, int[] editRoleIds, bool propagateToChildPages, bool propagateToChildSections)
		{
			Node node = this._nodeService.GetNodeById(id);
			try
			{
				this._nodeService.SetNodePermissions(node, viewRoleIds, editRoleIds, propagateToChildPages, propagateToChildSections);
				Messages.AddFlashMessageWithParams("PermissionsUpdatedMessage", node.Title);

			}
			catch (Exception ex)
			{
				Messages.AddFlashException(ex);
			}
			return RedirectToAction("Index", new { id = id });
		}

		#region AJAX actions

		public ActionResult GetPageListItem(int nodeId)
		{
			Node node = this._nodeService.GetNodeById(nodeId);
			return PartialView("PageListItem", node);
		}

		public ActionResult GetChildPageListItems(int nodeId)
		{
			Node node = this._nodeService.GetNodeById(nodeId);
			return PartialView("PageListItems", node.ChildNodes);
		}

		public ActionResult SelectPage(int nodeId)
		{
			Node node = this._nodeService.GetNodeById(nodeId);
			if (node.IsExternalLink)
			{
				ViewData["LinkTargets"] = new SelectList(EnumHelper.GetValuesWithDescription<LinkTarget>(), "Key", "Value", (int)node.LinkTarget);
				return PartialView("SelectedLink", node);
			}
			else
			{
				ViewData["Cultures"] = new SelectList(Globalization.GetOrderedCultures(), "Key", "Value", node.Culture);
				return PartialView("SelectedPage", node);
			}
		}

		[RolesFilter]
		public ActionResult RefreshTasks(int nodeId)
		{
			Node node = this._nodeService.GetNodeById(nodeId);
			ViewData["AvailableCultures"] = GetAvailableCultures();
			ViewData["NewLinkTargets"] = new SelectList(EnumHelper.GetValuesWithDescription<LinkTarget>(), "Key", "Value");
			return PartialView("Tasks", node);
		}

		[AcceptVerbs(HttpVerbs.Post)]
		public ActionResult SortPages(int parentNodeId, int[] orderedChildNodeIds)
		{
			AjaxMessageViewData result = new AjaxMessageViewData();
			try
			{
				this._nodeService.SortNodes(parentNodeId, orderedChildNodeIds);
				result.Message = GetText("PageOrderUpdatedMessage");
			}
			catch (Exception ex)
			{
				Logger.Error("Unexpected error while sorting pages.", ex);
				result.Error = ex.Message;
			}

			return Json(result);
		}

		public ActionResult ShowTemplate(int templateId)
		{
			Template template = this._templateService.GetTemplateById(templateId);
			return PartialView("PageTemplate", BuildTemplateViewData(template));
		}

		public ActionResult GetSectionsForPage(int nodeId)
		{
			IList sectionsForPage = new ArrayList();
			Node node = this._nodeService.GetNodeById(nodeId);
			foreach (Section section in node.Sections)
			{
				sectionsForPage.Add(new
                                 	{
										PlaceHolder = section.PlaceholderId,
                                 		SectionId = section.Id, 
										SectionName = section.Title, 
										ModuleType = section.ModuleType.Name, 
										Position = section.Position,
										EditUrl = section.ModuleType.EditPath ?? String.Empty
                                 	});
			}
			return Json(sectionsForPage);
		}

		[AcceptVerbs(HttpVerbs.Post)]
		public ActionResult SetTemplate(int nodeId, int templateId)
		{
			AjaxMessageViewData result = new AjaxMessageViewData();
			try
			{
				Node node = this._nodeService.GetNodeById(nodeId);
				node.Template = this._templateService.GetTemplateById(templateId);
				this._nodeService.SaveNode(node);
				result.Message = GetText("TemplateSetMessage");
			}
			catch (Exception ex)
			{
				Logger.Error("Unexpected error while setting template.", ex);
				result.Error = ex.Message;
			}
			JsonResult jsonResult = Json(result);
			jsonResult.ContentType = "text/html"; // otherwise the ajax form doesn't handle the callback
			return jsonResult;
		}

		#endregion

		private SelectList GetAvailableCultures()
		{
			// Get all cultures, but remove the cultures that are already connected to a root node.
			SortedList availableCultures = Globalization.GetOrderedCultures();
			foreach (Node rootNode in CuyahogaContext.CurrentSite.RootNodes)
			{
				availableCultures.Remove(rootNode.Culture);
			}
			return new SelectList(availableCultures, "Key", "Value");
		}

		private SectionViewData BuildSectionViewData(Section section)
		{
			var sectionViewData = new SectionViewData(section);
			var module = _moduleLoader.GetModuleFromSection(section);
			if (module is IActionProvider)
			{
				sectionViewData.OutboundActions = ((IActionProvider) module).GetOutboundActions();
			}
			if (module is IActionConsumer)
			{
				sectionViewData.InboundActions = ((IActionConsumer)module).GetInboundActions();
			}
			return sectionViewData;
		}

		private TemplateViewData BuildTemplateViewData(Template template)
		{
			string siteDataDir = CuyahogaContext.CurrentSite.SiteDataDirectory;
			string absoluteBasePath = VirtualPathUtility.Combine(siteDataDir, template.BasePath) + "/";
			string htmlContent = ViewUtil.RenderTemplateHtml(VirtualPathUtility.Combine(absoluteBasePath, template.TemplateControl));
			string cssContent = GetCssContent(absoluteBasePath + "Css/" + template.Css);
			TemplateViewData templateViewData = new TemplateViewData(template, htmlContent, cssContent);
			templateViewData.PrepareTemplateDataForEmbedding(Url.Content(CuyahogaContext.CurrentSite.SiteDataDirectory));
			return templateViewData;
		}

		private string GetCssContent(string virtualCssFilePath)
		{
			string physicalCssFilePath = Server.MapPath(virtualCssFilePath);

			using (Stream fileStream = this._fileService.ReadFile(physicalCssFilePath))
			using (StreamReader sr = new StreamReader(fileStream))
			{
				return sr.ReadToEnd();
			}
		}
	}
}
