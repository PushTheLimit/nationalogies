﻿<%@ Page Language="C#" MasterPageFile="~/Manager/Views/Shared/Admin.Master" Inherits="System.Web.Mvc.ViewPage<ICollection<Role>>" %>
<%@ Import Namespace="Cuyahoga.Core.Domain"%>
<%@ Import Namespace="Cuyahoga.Core.Service.Membership"%>
<asp:Content ID="Content1" ContentPlaceHolderID="cphHead" runat="server">
	<title>Cuyahoga Manager :: <%= GlobalResources.ManageRolesPageTitle %></title>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cphTasks" runat="server">
	<h2><%= GlobalResources.TasksLabel %></h2>
	<%= Html.ActionLink(GlobalResources.CreateRoleLabel, "NewRole", "Users", null, new { @class = "createlink" })%>
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="cphMain" runat="server">
	<h1><%= GlobalResources.ManageRolesPageTitle %></h1>
	<% if (ViewData.Model.Count > 0) { %>
		<table class="grid" style="width:100%">
			<thead>
				<tr>
					<th><%= GlobalResources.RoleLabel %></th>
					<th><%= GlobalResources.RightsLabel %></th>
					<th><%= GlobalResources.IsGlobalLabel%></th>
					<th>&nbsp;</th>
				</tr>
			</thead>
			<tbody>
				<% foreach (var role in ViewData.Model) { %>
					<tr>
						<td><%= role.Name %></td>
						<td><%= role.RightsString %></td>	
						<td><%= role.IsGlobal %></td>
						<td style="white-space:nowrap">
							<%= Html.ActionLink(GlobalResources.UsersLabel, "Browse", new { roleid = role.Id }, new { @class = "abtnbrowseuser" })%>
							<% if (! role.IsGlobal || Html.HasRight(User, Rights.GlobalPermissions)) { %>
								<%= Html.ActionLink(GlobalResources.EditLabel, "EditRole", new { id = role.Id }, new { @class = "abtnedit" }) %>
								<% using (Html.BeginForm("DeleteRole", "Users", new { id = role.Id }, FormMethod.Post))
           { %>
									<a href="#" class="abtndelete"><%= GlobalResources.DeleteButtonLabel %></a>
								<% } %>
							<% } %>
						</td>
					</tr>
				<% } %>
			</tbody>
		</table>
	<% } else { %>
		<%= GlobalResources.NoRolesFound %>
	<% } %>
</asp:Content>
