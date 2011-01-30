﻿<%@ Page Language="C#" MasterPageFile="~/Manager/Views/Shared/Admin.Master" Inherits="System.Web.Mvc.ViewPage<Template>" %>
<%@ Import Namespace="Cuyahoga.Core.Domain"%>
<asp:Content ID="Content1" ContentPlaceHolderID="cphHead" runat="server">
	<title>Cuyahoga Manager :: <%= GlobalResources.RegisterTemplatePageTitle %></title>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="cphTasks" runat="server">
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderID="cphMain" runat="server">
	<h1><%= GlobalResources.RegisterTemplatePageTitle %></h1>
	<script type="text/javascript">
		$(document).ready(function() {
			$('#BasePath').change(function() {
				$('#TemplateControl').html('');
				$('#Css').html('');
				$.getJSON('<%= Url.Action("GetTemplateControlsForBasePath", "Templates") %>', { basepath:$('#BasePath').val() }, function(data) {
					$.each(data, function(i, item) {
						$('#TemplateControl').append('<option value="' + item.TemplateControl + '">' + item.TemplateControl + '</option>');
					});
				});
				$.getJSON('<%= Url.Action("GetCssFilesForBasePath", "Templates") %>', { basepath:$('#BasePath').val() }, function(data) {
					$.each(data, function(i, item) {
						$('#Css').append('<option value="' + item.CssFile + '">' + item.CssFile + '</option>');
					});
				});
			});
		});
	</script>
	<% using (Html.BeginForm("Create", "Templates", FormMethod.Post, new { id = "templateform" })) { %>
		<fieldset>
			<legend><%=GlobalResources.GeneralLabel%></legend>
			<ol>
				<li>
					<label for="Name"><%=GlobalResources.NameLabel%></label>
					<%=Html.TextBox("Name", ViewData.Model.Name, new { style = "width:300px" })%>
				</li>
				<li>
					<label for="BasePath"><%=GlobalResources.BasePathLabel %></label>
					<%=Html.DropDownList("BasePath", ViewData["BasePaths"] as SelectList)%>
				</li>
				<li>
					<label for="TemplateControl"><%=GlobalResources.TemplateControlLabel %></label>
					<%=Html.DropDownList("TemplateControl", ViewData["TemplateControls"] as SelectList)%>
				</li>
				<li>
					<label for="Css"><%=GlobalResources.CssLabel %></label>
					<%=Html.DropDownList("Css", ViewData["CssFiles"] as SelectList)%>
				</li>
			</ol>
		</fieldset>
		
		<div id="buttonpanel">
		    <input type="submit" class="abtnsave" value="<%= GlobalResources.SaveButtonLabel %>" /> <strong><%= GlobalResources.Or %></strong> <%= Html.ActionLink(GlobalResources.CancelLabel, "Index", null, new { @class = "abtncancel" })%>
		</div>	
		
	<% } %>
</asp:Content>
