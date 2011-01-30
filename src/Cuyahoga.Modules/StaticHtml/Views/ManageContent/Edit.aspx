﻿<%@ Page Title="" Language="C#" MasterPageFile="~/Modules/Shared/Views/Shared/ModuleAdmin.Master" Inherits="System.Web.Mvc.ViewPage<ModuleAdminViewModel<StaticHtmlContent>>" %>
<%@ Import Namespace="Cuyahoga.Web.Mvc.HtmlEditor"%>
<%@ Import Namespace="Cuyahoga.Web.Mvc.ViewModels"%>
<%@ Import Namespace="Cuyahoga.Modules.StaticHtml"%>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
	<title><%= GlobalResources.EditContentPageTitle %></title>
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="HeaderContent" runat="server">
	<h2><%= GlobalResources.EditContentPageTitle %></h2>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="MainContent" runat="server">
	<% using (Html.BeginForm("SaveContent", "ManageContent", Model.GetNodeAndSectionParams(), FormMethod.Post)) { 
		string contentCss = Model.Node != null && Model.Node.Template != null
			? Url.Content(Model.CuyahogaContext.CurrentSite.SiteDataDirectory) + Model.Node.Template.EditorCss
			: null;
		%>
		<%= Html.HtmlEditor("content", Model.ModuleData.Content, contentCss, new { style = "width:100%;height:340px"}) %>
		<input type="submit" value="Save" />
		<% Html.RenderPartial("Categories", Model.ModuleData); %>
	<% } %>
</asp:Content>
