namespace NewHarvestPatches
{
    internal class AddOrReplace : PatchOperationPathedExtended
    {
        /// <summary>
        /// Adds or replaces a node.
        /// </summary>

        private readonly XmlContainer value = null;

        protected override bool ApplyWorker(XmlDocument xml)
        {
            try
            {
                if (value == null)
                    return false;

                XmlNode node = value.node;
                if (node == null)
                    return false;

                if (!PreCheck(xpath, xml))
                    return false;

                bool modified = false;
                XmlNode foundNode = null;
                foreach (XmlNode xmlNode in nodes)
                {
                    foreach (XmlNode addNode in node.ChildNodes)
                    {
                        if (ContainsNode(xmlNode, addNode, ref foundNode))
                        {
                            // Replace
                            XmlNode importedNode = xmlNode.OwnerDocument.ImportNode(addNode, true);
                            xmlNode.ReplaceChild(importedNode, foundNode);
                            modified = true;
                            ToLog($"Replaced node <{foundNode.Name}> (old value: {foundNode.InnerXml}) with <{addNode.Name}> (new value: {addNode.InnerXml}) in <{xmlNode.Name}>.");
                        }
                        else
                        {
                            // Add
                            xmlNode.AppendChild(xmlNode.OwnerDocument.ImportNode(addNode, true));
                            modified = true;
                            ToLog($"Added node <{addNode.Name}> (value: {addNode.InnerXml}) to <{xmlNode.Name}>.");
                        }
                    }
                }
                return modified;
            }
            catch (Exception ex)
            {
                ExToLog(ex, MethodBase.GetCurrentMethod(), optMsg: $"{xpath}");
                return false;
            }
        }
    }
}