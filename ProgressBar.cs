/*

NeatUpload - an HttpModule and User Controls for uploading large files
Copyright (C) 2005  Dean Brettle

This library is free software; you can redistribute it and/or
modify it under the terms of the GNU Lesser General Public
License as published by the Free Software Foundation; either
version 2.1 of the License, or (at your option) any later version.

This library is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public
License along with this library; if not, write to the Free Software
Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
*/

using System;
using System.Collections;
using System.IO;
using System.ComponentModel;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;

namespace Brettle.Web.NeatUpload
{
	/// <summary>
	/// Displays progress and status of an upload.</summary>
	/// <remarks>
	/// For the progress bar to be displayed, the <see cref="UploadHttpModule"/> must be in use.
	/// For the progress display to be started, the form being submitted must include an <see cref="InputFile"/>
	/// control that is not empty.  Use the <see cref="Inline"/> property to control how the progress bar is
	/// displayed.  Use the <see cref="NonUploadButtons"/> property (or the <see cref="AddNonUploadButton"/> method)
	/// to specify any buttons which should not cause files to be uploaded and should not start the progress
	/// display (e.g. "Cancel" buttons).
	/// </remarks>
	[DefaultProperty("Inline"), ParseChildren(false), PersistChildren(true)]
	public class ProgressBar : System.Web.UI.WebControls.WebControl
	{
		private bool IsDesignTime = (HttpContext.Current == null);

		private string uploadProgressUrl;
		private string displayStatement;
		private ArrayList otherNonUploadButtons = new ArrayList(); // Controls passed to AddNonUploadButton()
		private ArrayList otherTriggers = new ArrayList(); // Controls passed to AddTrigger()
		private	HtmlTextWriterTag Tag;

		/// <summary>
		/// Whether to display the progress bar inline or as a pop-up.</summary>
		[DefaultValue(false)]
		public bool Inline
		{
			get { return (ViewState["inline"] != null && (bool)ViewState["inline"]); }
			set { ViewState["inline"] = value; }
		}
		
		/// <summary>
		/// Space-separated list of the IDs of controls which should not upload files and start the progress 
		/// display. </summary>
		/// <remarks>
		/// When a user clicks on a non-upload control, Javascript clears all <see cref="InputFile" /> controls. 
		/// As a result, the progress display does not start and no files are uploaded when the form is submitted.
		/// If no triggers are listed in <see cref="Triggers"/> or added via <see cref="AddTrigger"/> then any control
		/// other than those listed in <see cref="NonUploadButtons"/> or added via <see cref="AddNonUploadButton"/>
		/// will be considered a trigger and will upload files and start the progress display.  If you do specify
		/// one or more triggers, then all links and submit buttons <i>other</i> than those triggers will be considered
		/// non-upload controls (in addition to any controls listed in <see cref="NonUploadButtons"/> or added via
		/// <see cref="AddNonUploadButton"/>).  This means that in most cases you can simply specify one or more
		/// triggers and not worry about specifying non-upload controls unless you have controls other than links and
		/// submit buttons that cause the form to submit.</remarks>  
		public string NonUploadButtons
		{
			get { return (string)ViewState["NonUploadButtons"]; }
			set { ViewState["NonUploadButtons"] = value; }
		}

		/// <summary>
		/// Space-separated list of the IDs of controls which should upload files and start the progress 
		/// display. </summary>
		/// <remarks>
		/// If no triggers are listed in <see cref="Triggers"/> or added via <see cref="AddTrigger"/> then any control
		/// other than those listed in <see cref="NonUploadButtons"/> or added via <see cref="AddNonUploadButton"/>
		/// will be considered a trigger and will upload files and start the progress display.  If you do specify
		/// one or more triggers, then all links and submit buttons <i>other</i> than those triggers will be considered
		/// non-upload controls (in addition to any controls listed in <see cref="NonUploadButtons"/> or added via
		/// <see cref="AddNonUploadButton"/>).  This means that in most cases you can simply specify one or more
		/// triggers and not worry about specifying non-upload controls unless you have controls other than links and
		/// submit buttons that cause the form to submit.</remarks>  
		public string Triggers
		{
			get { return (string)ViewState["Triggers"]; }
			set { ViewState["Triggers"] = value; }
		}
		
		protected override void OnInit(EventArgs e)
		{
			InitializeComponent();
			base.OnInit(e);
		}
		
		private void InitializeComponent()
		{
			if (IsDesignTime || !Config.Current.UseHttpModule)
				return;
			string appPath = Context.Request.ApplicationPath;
			if (appPath == "/")
			{
				appPath = "";
			}
			uploadProgressUrl = Attributes["src"];
			if (uploadProgressUrl == null)
				uploadProgressUrl = appPath + "/NeatUpload/Progress.aspx";

			uploadProgressUrl += "?postBackID=" + FormContext.Current.PostBackID;

			if (Attributes["class"] == null)
			{
				Attributes["class"] = "ProgressBar";
			}

			if (UploadContext.Current != null)
			{
				uploadProgressUrl += "&lastPostBackID=" + UploadContext.Current.PostBackID;
			}
			
			if (Inline)
			{
				Tag = HtmlTextWriterTag.Iframe;
				Attributes["src"] = uploadProgressUrl;
				Attributes["frameborder"] = "0";
				Attributes["scrolling"] = "no";
				Attributes["name"] = this.ClientID;
				displayStatement = @"
setTimeout(function () {
	frames['" + this.ClientID + @"'].location.href = '" + uploadProgressUrl + @"&refresher=client';
}, 0);
";
			}
			else
			{
				Tag = HtmlTextWriterTag.Div;
				displayStatement = @"
window.open('" + uploadProgressUrl + "&refresher=client', '" + FormContext.Current.PostBackID + @"',
				'width=500,height=100,directories=no,location=no,menubar=no,resizable=yes,scrollbars=no,status=no,toolbar=no');
";
				this.Page.RegisterStartupScript(this.UniqueID + "RemoveDiv", @"
<script language=""javascript"">
<!--
NeatUpload_DivNode = document.getElementById('" + this.ClientID + @"'); 
if (NeatUpload_DivNode)
	NeatUpload_DivNode.parentNode.removeChild(NeatUpload_DivNode);
-->
</script>
");
			}
		}
		
		/// <summary>
		/// Adds a control (typically a button) to a list trigger controls.</summary>
		/// <param name="control">the control to add to the list</param>
		/// <remarks>
		/// See the <see cref="Triggers"/> property for information on what triggers are.  This method is
		/// primarily for situations where the see cref="Triggers"/> property can't be used because the ID of the
		/// trigger control is not known until runtime (e.g. for
		/// controls in Repeaters).  Controls added via this method are maintained in a separate list from those
		/// listed in the <see cref="Triggers"/> property, and said list is not maintained as part of this
		/// control's <see cref="ViewState"/>.  That means that if you use this method, you will need to call it
		/// for each request, not just non-postback requests.  Also, you can use both this method and the
		/// <see cref="Triggers"/> property for the same control.
		/// </remarks>
		public void AddTrigger(Control control)
		{
			otherTriggers.Add(control);
		}

		/// <summary>
		/// Adds a control (typically a button) to a list non-upload controls.</summary>
		/// <param name="control">the control to add to the list</param>
		/// <remarks>
		/// See the <see cref="NonUploadButtons"/> property for information on what non-upload buttons are.
		/// This method is primarily for situations where the see cref="NonUploadButtons"/> property can't be used
		/// because the ID of the non-upload control is not known until runtime (e.g. for
		/// controls in Repeaters).  Controls added via this method are maintained in a separate list from those
		/// listed in the <see cref="NonUploadButtons"/> property, and said list is not maintained as part of this
		/// control's <see cref="ViewState"/>.  That means that if you use this method, you will need to call it
		/// for each request, not just non-postback requests.  Also, you can use both this method and the
		/// <see cref="NonUploadButtons"/> property for the same control.
		/// </remarks>
		public void AddNonUploadButton(Control control)
		{
			otherNonUploadButtons.Add(control);
		}

		protected override void OnPreRender (EventArgs e)
		{
			if (IsDesignTime || !Config.Current.UseHttpModule)
				return;

			ArrayList nonUploadButtonIDs = new ArrayList(); // IDs of buttons refed by NonUploadButtons property
			if (NonUploadButtons != null)
			{
				nonUploadButtonIDs.AddRange(NonUploadButtons.Split(' '));
			}
			if (nonUploadButtonIDs.Count + otherNonUploadButtons.Count > 0)
			{
				foreach (string buttonID in nonUploadButtonIDs)
				{
					Control c = NamingContainer.FindControl(buttonID);
					if (c == null)
						continue;
					RegisterNonUploadButtonScripts(c);
				}
				foreach (Control c in otherNonUploadButtons)
				{
					RegisterNonUploadButtonScripts(c);
				}
			}
			ArrayList triggerIDs = new ArrayList(); // IDs of controls refed by Triggers property
			if (Triggers != null)
			{
				triggerIDs.AddRange(Triggers.Split(' '));
			}
			foreach (string buttonID in triggerIDs)
			{
				Control c = NamingContainer.FindControl(buttonID);
				if (c == null)
					continue;
				RegisterTriggerScripts(c);
			}
			foreach (Control c in otherTriggers)
			{
				RegisterTriggerScripts(c);
			}
			HtmlControl formControl = GetFormControl(this);
			RegisterScriptsForForm(formControl);
		}

		protected override void Render(HtmlTextWriter writer)
		{
			if (IsDesignTime)
			{
				Tag = HtmlTextWriterTag.Div;
			}
			else if (!Config.Current.UseHttpModule)
				return;
			EnsureChildControls();
			base.AddAttributesToRender(writer);
			writer.RenderBeginTag(Tag);
			if (IsDesignTime)
			{
				if (Inline)
				{
					writer.Write("<i>Inline ProgressBar - no-IFRAME fallback = {</i>");
				}
				else
				{
					writer.Write("<i>Pop-up ProgressBar - no-Javascript fallback = {</i>");
				}
			}
			writer.AddAttribute("href", uploadProgressUrl + "&refresher=server");
			writer.AddAttribute("target", FormContext.Current.PostBackID);
			writer.RenderBeginTag(HtmlTextWriterTag.A);
			if (!HasControls())
			{
				writer.Write("Check Upload Progress");
			}
			base.RenderChildren(writer);
			writer.RenderEndTag();
			if (IsDesignTime)
			{
				writer.Write("<i>}</i>");
			}
			writer.RenderEndTag();
		}
		
		private void RegisterNonUploadButtonScripts(Control control)
		{
			if (!Config.Current.UseHttpModule)
				return;
			
			HtmlControl formControl = GetFormControl(control);
			
			RegisterScriptsForForm(formControl);
			this.Page.RegisterStartupScript(this.UniqueID + "-AddNonUploadButton-" + control.UniqueID, @"
<script language=""javascript"">
<!--
NeatUpload_NonUploadIDs_" + this.ClientID + @"['" + control.ClientID + @"'] 
	= ++NeatUpload_NonUploadIDs_" + this.ClientID + @".NeatUpload_length;
// -->
</script>
");			
		}

		private void RegisterTriggerScripts(Control control)
		{
			if (!Config.Current.UseHttpModule)
				return;
			
			HtmlControl formControl = GetFormControl(control);
			
			RegisterScriptsForForm(formControl);

			this.Page.RegisterStartupScript(this.UniqueID + "-AddTrigger-" + control.UniqueID, @"
<script language=""javascript"">
<!--
NeatUpload_TriggerIDs_" + this.ClientID + @"['" + control.ClientID + @"'] 
	= ++NeatUpload_TriggerIDs_" + this.ClientID + @".NeatUpload_length;
-->
</script>
");			
		}

		private HtmlControl GetFormControl(Control control)
		{
			HtmlControl formControl = null;
			for (Control c = control; c != null; c=c.Parent)
			{
				formControl = c as HtmlControl;
				if (formControl != null && String.Compare(formControl.TagName, "FORM", true) == 0)
					break;
			}
			return formControl;
		}

		private void RegisterScriptsForForm(Control formControl)
		{
			this.Page.RegisterStartupScript(formControl.UniqueID + "-OnSubmit", @"
<script language=""javascript"">
<!--
function NeatUpload_OnSubmitForm_" + formControl.ClientID + @"()
{
	var elem = document.getElementById('" + formControl.ClientID + @"');
	elem.NeatUpload_OnSubmit();
}

document.getElementById('" + formControl.ClientID + @"').onsubmit 
	= NeatUpload_CombineHandlers(document.getElementById('" + formControl.ClientID + @"').onsubmit, NeatUpload_OnSubmitForm_" + formControl.ClientID + @");
-->
</script>
");

			this.Page.RegisterStartupScript(this.UniqueID + "-AddHandler", @"
<script language=""javascript"">
<!--
NeatUpload_AddSubmitHandler('" + formControl.ClientID + "'," + (Inline ? "false" : "true") + @", function () {
	if (NeatUpload_IsFilesToUpload('" + formControl.ClientID + @"'))
	{
		" + displayStatement + @"
	}
});

NeatUpload_NonUploadIDs_" + this.ClientID + @" = new Object();
NeatUpload_NonUploadIDs_" + this.ClientID + @".NeatUpload_length = 0;
NeatUpload_TriggerIDs_" + this.ClientID + @" = new Object();
NeatUpload_TriggerIDs_" + this.ClientID + @".NeatUpload_length = 0;
						
NeatUpload_AddHandler('" + formControl.ClientID + @"', 'click', function (ev) {
	if (!ev)
	{
		return;
	}
	var src = null;
	if (ev.srcElement)
		src = ev.srcElement;
	else if (ev.target)
		src = ev.target;
	if (src == null)
	{
		return;
	}
	var tagName = src.tagName;
	if (tagName) tagName = tagName.toLowerCase();
	if (NeatUpload_TriggerIDs_" + this.ClientID + @"[src.id])
	{
		return;
	}
	var formElem = document.getElementById('" + formControl.ClientID + @"');
	if (NeatUpload_NonUploadIDs_" + this.ClientID + @"[src.id])
	{
		NeatUpload_ClearFileInputs(formElem);
	}
	else if (NeatUpload_TriggerIDs_" + this.ClientID + @".NeatUpload_length == 0)
	{
		return;
	}
	else if (tagName == 'input' || tagName == 'button')
	{
		var inputType = src.getAttribute('type');
		if (inputType) inputType = inputType.toLowerCase();
		if (!inputType || inputType == 'submit' || inputType == 'image')
		{
			NeatUpload_ClearFileInputs(formElem);
		}
	}
	else if (tagName == 'a' && !src.getAttribute('name')) 
	{
		NeatUpload_ClearFileInputs(formElem);
	}
});
-->
</script>
");
			if (!this.Page.IsClientScriptBlockRegistered("NeatUploadProgressBar"))
			{
				this.Page.RegisterClientScriptBlock("NeatUploadProgressBar", clientScript);
			}
		}

		
		private string clientScript = @"
<script language=""javascript"">
<!--
function NeatUpload_CombineHandlers(origHandler, newHandler) 
{
	if (!origHandler || typeof(origHandler) == 'undefined') return newHandler;
	return function(e) { if (origHandler(e) == false) return false; return newHandler(e); };
};
function NeatUpload_AddHandler(id, eventName, handler)
{
	var elem = document.getElementById(id);
	if (elem.addEventListener)
	{
		elem.addEventListener(eventName, handler, false);
	}
	else if (elem.attachEvent)
	{
		elem.attachEvent(""on"" + eventName, handler);
	}
	else
	{
		elem[""on"" + eventName] = NeatUpload_CombineHandlers(elem[""on"" + eventName], handler);
	}
}
function NeatUpload_IsFilesToUpload(id)
{
	var formElem = document.getElementById(id);
	while (formElem && formElem.tagName.toLowerCase() != ""form"")
	{
		formElem = formElem.parent;
	}
	if (!formElem) 
	{
		return false;
	}
	var inputElems = formElem.getElementsByTagName(""input"");
	var foundFileInput = false;
	var isFilesToUpload = false;
	for (i = 0; i < inputElems.length; i++)
	{
		var inputElem = inputElems.item(i);
		if (inputElem && inputElem.type && inputElem.type.toLowerCase() == ""file"")
		{
			foundFileInput = true;
			if (inputElem.value && inputElem.value.length > 0)
			{
				isFilesToUpload = true;

				// If the browser really is IE on Windows, return false if the path is not absolute because
				// IE will not actually submit the form if any file value is not an absolute path.  If IE doesn't
				// submit the form, any progress bars we start will never finish.  
				if (navigator && navigator.userAgent
					&& navigator.userAgent.toLowerCase().indexOf('msie') != -1 && typeof(ActiveXObject) != 'undefined') 
				{
					var re = new RegExp('^(\\\\\\\\[^\\\\]|([a-zA-Z]:)?\\\\).*');
					var match = re.exec(inputElem.value);
					if (match == null || match[0] == '')
						return false;
				}
			}
		}
	}
	return isFilesToUpload; 
}

function NeatUpload_ClearFileInputs(elem)
{
	var inputFiles = elem.getElementsByTagName('input');
	for (var i=0; i < inputFiles.length; i++ )
	{
		var inputFile = inputFiles.item(i);
		if (inputFile.type == 'file')
		{
			var newInputFile = document.createElement('input');
			for (var a=0; a < inputFile.attributes.length; a++)
			{
				var attr = inputFile.attributes.item(a); 
				if (attr.specified && attr.name != 'type' && attr.name != 'value')
					newInputFile.setAttribute(attr.name, attr.value);
			}
			newInputFile.setAttribute('type', 'file');
			inputFile.parentNode.replaceChild(newInputFile, inputFile);
		}
	}
}

function NeatUpload_AddSubmitHandler(formID, isPopup, handler)
{
	var elem = document.getElementById(formID);
	if (!elem.NeatUpload_OnSubmitHandlers) 
	{
		elem.NeatUpload_OnSubmitHandlers = new Array();
		elem.NeatUpload_OrigSubmit = elem.submit;
		elem.NeatUpload_OnSubmit = NeatUpload_OnSubmit;
		elem.submit = function () {
			elem.NeatUpload_OrigSubmit();
			elem.NeatUpload_OnSubmit();
		};
	}
	if (isPopup)
	{
		elem.NeatUpload_OnSubmitHandlers.unshift(handler);
	}
	else
	{
		elem.NeatUpload_OnSubmitHandlers.push(handler);
	}	
}

function NeatUpload_OnSubmit()
{
	for (var i=0; i < this.NeatUpload_OnSubmitHandlers.length; i++)
	{
		this.NeatUpload_OnSubmitHandlers[i].call(this);
	}
	return true;
}
-->
</script>
";
	}
}
