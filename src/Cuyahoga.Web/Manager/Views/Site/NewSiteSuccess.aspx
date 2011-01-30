﻿<%@ Page Language="C#" MasterPageFile="~/Manager/Views/Shared/Admin.Master" Inherits="System.Web.Mvc.ViewPage<Site>" %>
<%@ Import Namespace="Cuyahoga.Core.Domain"%>
<asp:Content ID="Content1" ContentPlaceHolderID="cphHead" runat="server">
	<title>Cuyahoga Manager :: <%= GlobalResources.NewSiteSuccessPageTitle %></title>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cphTasks" runat="server">
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="cphMain" runat="server">
	<h1><%= GlobalResources.NewSiteSuccessPageTitle %></h1>
	<fieldset>
		<ol>
			<li>
				<label><%= GlobalResources.SiteLabel %></label>
				<%= ViewData.Model.Name %>
			</li>
			<li>
				<label><%= GlobalResources.SiteUrlLabel %></label>
				<%= ViewData.Model.SiteUrl %>
			</li>
		</ol>
	</fieldset>
	<p>
		<%= Html.ActionLink(GlobalResources.JumpToSiteAdminLabel, "SetSite", "Dashboard", new RouteValueDictionary { {"siteId", ViewData.Model.Id} }, null )%><br />
		<%= GlobalResources.NewSiteWarningText %>
	</p>
</asp:Content>
