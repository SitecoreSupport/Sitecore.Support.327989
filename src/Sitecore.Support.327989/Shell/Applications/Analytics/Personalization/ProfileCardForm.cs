namespace Sitecore.Support.Shell.Applications.Analytics.Personalization
{
  using Sitecore.Analytics.Data;
  using Sitecore.Data.Fields;
  using Sitecore.Data.Items;
  using Sitecore.Framework.Conditions;
  using Sitecore.Shell.Applications.Analytics.Personalization;
  using Sitecore.Web;
  using System;
  using System.Collections.Generic;
  using System.Web.UI.HtmlControls;
  using System.Linq;
  using System.Web.UI.WebControls;
  using System.Web;
  using System.Text;
  using Sitecore.Security.AccessControl;
  using Sitecore.Data;
  using Sitecore.Globalization;
  using Sitecore.Diagnostics;

  public class ProfileCardForm : Sitecore.Shell.Applications.Analytics.Personalization.ProfileCardForm
  {
    protected new void Page_Load(object sender, EventArgs e)
    {
      UrlHandle urlHandle = UrlHandle.Get();
      DatabaseName = StringUtil.GetString(urlHandle["db"]);
      FieldID = StringUtil.GetString(urlHandle["fid"]);
      ItemID = StringUtil.GetString(urlHandle["iid"]);
      ItemVersion = StringUtil.GetString(urlHandle["ver"]);
      ItemLanguage = StringUtil.GetString(urlHandle["lang"]);
      ReadOnly = (StringUtil.GetString(urlHandle["ro"]) == "1");
      HtmlTable htmlTable = new HtmlTable();
      Controls.Add(htmlTable);
      htmlTable.Style["width"] = "100%";
      htmlTable.Style["table-layout"] = "fixed";
      Dictionary<string, TextBoxSlider> dictionary = new Dictionary<string, TextBoxSlider>();
      try
      {
        Item profileItem = GetProfileItem();
        Field field = GetCurrentItem().Fields[FieldID];
        Condition.Ensures(field, "field not found").IsNotNull();
        ContentProfile contentProfile = new Sitecore.Analytics.Data.TrackingField(field).Profiles.FirstOrDefault((ContentProfile profileData) => profileData.ProfileID == profileItem.ID);
        Condition.Ensures(contentProfile, "profile").IsNotNull();
        HtmlTableRow htmlTableRow = new HtmlTableRow();
        htmlTable.Rows.Add(htmlTableRow);
        HtmlTableCell htmlTableCell = new HtmlTableCell();
        HtmlTable htmlTable2 = new HtmlTable
        {
          Width = "100%"
        };
        ContentProfileKeyData[] sortedProfileKeys = ProfileUtil.GetSortedProfileKeys(contentProfile);
        foreach (ContentProfileKeyData contentProfileKeyData in sortedProfileKeys)
        {
          HtmlTableRow htmlTableRow2 = new HtmlTableRow();
          HtmlTableCell htmlTableCell2 = new HtmlTableCell();
          htmlTableCell2.Attributes.Add("class", "profileKeyName");
          Label label = new Label();

          // Sitecore.Support.Fix +++
          string keyLabel = contentProfileKeyData.DisplayName;
          
          if (string.IsNullOrWhiteSpace(keyLabel))
          {
            Log.Debug("[Sitecore.Support.327989] 'contentProfileKeyData.DisplayName' is null or empty, the 'contentProfileKeyData.Key' value is used", this);
            keyLabel = contentProfileKeyData.Key;
          }

          label.Text = keyLabel + ":";
          //label.Text = contentProfileKeyData.DisplayName + ":";
          // Sitecore.Support. Fix ---
          htmlTableCell2.Controls.Add(label);
          htmlTableRow2.Cells.Add(htmlTableCell2);
          htmlTableCell2 = new HtmlTableCell();
          htmlTableCell2.Attributes.Add("class", "profileKeyControl");
          TextBoxSlider textBoxSlider = new TextBoxSlider();
          textBoxSlider.Width = 150;
          textBoxSlider.Height = 25;
          textBoxSlider.Value = contentProfileKeyData.Value.ToString();
          textBoxSlider.MinimumValue = contentProfileKeyData.MinValue.ToString();
          textBoxSlider.MaximumValue = contentProfileKeyData.MaxValue.ToString();
          textBoxSlider.DisplayName = HttpUtility.HtmlDecode(contentProfileKeyData.DisplayName);
          textBoxSlider.SliderHeight = 40;
          textBoxSlider.Height = 40;
          textBoxSlider.AllowOverflowValue = (Sitecore.Context.User.IsAdministrator || (profileItem != null && AuthorizationManager.IsAllowed(profileItem, AccessRight.ProfileCustomize, Sitecore.Context.User)));
          htmlTableCell2.Controls.Add(textBoxSlider);
          dictionary.Add(new ID(contentProfileKeyData.ProfileKeyDefinitionId).ToShortID().ToString(), textBoxSlider);
          htmlTableRow2.Cells.Add(htmlTableCell2);
          htmlTable2.Rows.Add(htmlTableRow2);
        }
        htmlTableCell.Controls.Add(htmlTable2);
        htmlTableRow.Cells.Add(htmlTableCell);
      }
      catch (Exception exception)
      {
        Log.Error("Exception during rendering Profile Card Value field.", exception, this);
        HtmlTableRow htmlTableRow = new HtmlTableRow();
        htmlTable.Rows.Add(htmlTableRow);
        HtmlTableCell htmlTableCell = new HtmlTableCell();
        htmlTableCell.InnerText = Translate.Text("No profiles found.");
        htmlTableRow.Cells.Add(htmlTableCell);
      }
      ProfileKeyContainer.Controls.Add(htmlTable);
      Chart.SubscribeTo(dictionary.Values.ToArray());
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append("<script type=\"text/javascript\">");
      stringBuilder.Append("function scGetFrameValue(value, request) {");
      stringBuilder.Append("var result = \"\";");
      List<string> list = new List<string>(dictionary.Keys);
      foreach (string item in list)
      {
        stringBuilder.Append(dictionary[item].JavaControlId + ".validateSliderData();");
      }
      foreach (string item2 in list)
      {
        stringBuilder.Append("result += \"" + item2 + "|\" + " + dictionary[item2].JavaControlId + ".getValue() + \"||\";");
        string text = dictionary[item2].OnClientChanged;
        if (string.IsNullOrEmpty(text))
        {
          text = "null";
        }
        dictionary[item2].OnClientChanged = string.Format("function(data) {0}SetModified(); var func = {2}; if(func != null){0}func(data);{1}{1}", "{", "}", text);
        dictionary[item2].Enabled = !ReadOnly;
      }
      stringBuilder.Append("return result;");
      stringBuilder.Append("}");
      stringBuilder.Append("</script>");
      Page.ClientScript.RegisterStartupScript(GetType(), "ProfileCardValue", stringBuilder.ToString());
    }
  }
}
