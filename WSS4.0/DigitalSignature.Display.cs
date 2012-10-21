using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Text;
using System.Globalization;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.Utilities;

namespace zmeng.SharePoint
{
	public partial class DigitalSignatureDisplay : System.Web.UI.Page
	{
		protected void Page_Load(object sender, EventArgs e)
		{
			string listId = Request.QueryString.Get("ListId").ToString();
			string itemId = Request.QueryString.Get("ItemId").ToString();
			string displayName = Request.QueryString.Get("FieldDisplayName").ToString();

			Guid listGuid = new Guid(listId);
			int itemIndex = Int32.Parse(itemId);

			SPList spList = SPContext.Current.Web.Lists[listGuid];
			SPListItem spListItem = spList.GetItemById(itemIndex);

			string output = Utilities.RenderSignatureBlock(spListItem.Versions[0], displayName, false);
            
            if (output.Length > 0)
				Page.Response.Write("document.write('" + output.Replace(@"\", @"\\").Replace("'", @"\'") + "');");

			Page.Response.Flush();
		}
	}
}