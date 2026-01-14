using System.Text.RegularExpressions;

namespace NewHarvestPatches
{
    internal class EditString : PatchOperationPathedExtended
    {
        public enum StringEditMode
        {
            ReplaceSubstring,
            PrependWhole,
            AppendWhole,
            PrependSubstring,
            AppendSubstring
        }

        public StringEditMode mode = StringEditMode.ReplaceSubstring;
        public string substring;
        public string replaceWith;
        public bool ignoreCase;

        protected override bool ApplyWorker(XmlDocument xml)
        {
            // Ensure replaceWith is at least empty to avoid null concatenation
            replaceWith ??= string.Empty;

            // Ensure xpath/nodes are validated and populated before using `nodes`
            if (!PreCheck(xpath, xml))
                return false;

            // For modes that operate on a specific substring, default substring to the node text
            if (mode == StringEditMode.ReplaceSubstring
                || mode == StringEditMode.PrependSubstring
                || mode == StringEditMode.AppendSubstring)
            {
                substring ??= nodes.First().InnerText;
            }

            // Prepare regex when substring-based operations are used.
            Regex regex = null;
            if (!string.IsNullOrEmpty(substring)
                && (mode == StringEditMode.ReplaceSubstring
                    || mode == StringEditMode.PrependSubstring
                    || mode == StringEditMode.AppendSubstring))
            {
                var options = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
                regex = new Regex(Regex.Escape(substring), options);
            }

            foreach (XmlNode xmlNode in nodes)
            {
                if (xmlNode.NodeType != XmlNodeType.Element)
                    continue;

                switch (mode)
                {
                    case StringEditMode.PrependWhole:
                        xmlNode.InnerText = string.IsNullOrEmpty(replaceWith)
                            ? xmlNode.InnerText
                            : replaceWith + " " + xmlNode.InnerText;
                        break;

                    case StringEditMode.AppendWhole:
                        xmlNode.InnerText = string.IsNullOrEmpty(replaceWith)
                            ? xmlNode.InnerText
                            : xmlNode.InnerText + " " + replaceWith;
                        break;

                    case StringEditMode.PrependSubstring:
                        if (regex != null && regex.IsMatch(xmlNode.InnerText))
                        {
                            // Insert replaceWith immediately before every matched substring occurrence
                            xmlNode.InnerText = regex.Replace(xmlNode.InnerText, m =>
                                (string.IsNullOrEmpty(replaceWith) ? string.Empty : replaceWith + " ") + m.Value);
                        }
                        break;

                    case StringEditMode.AppendSubstring:
                        if (regex != null && regex.IsMatch(xmlNode.InnerText))
                        {
                            // Insert replaceWith immediately after every matched substring occurrence
                            xmlNode.InnerText = regex.Replace(xmlNode.InnerText, m =>
                                m.Value + (string.IsNullOrEmpty(replaceWith) ? string.Empty : " " + replaceWith));
                        }
                        break;

                    case StringEditMode.ReplaceSubstring:
                    default:
                        if (regex != null && regex.IsMatch(xmlNode.InnerText))
                        {
                            xmlNode.InnerText = regex.Replace(xmlNode.InnerText, replaceWith);
                        }
                        break;
                }

                // Ensure there is no leading whitespace after the edit.
                if (!string.IsNullOrEmpty(xmlNode.InnerText) && char.IsWhiteSpace(xmlNode.InnerText[0]))
                {
                    xmlNode.InnerText = xmlNode.InnerText.TrimStart();
                }
            }
            return true;
        }
    }
}