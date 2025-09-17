namespace NewHarvestPatches
{
    internal class AddOrReplaceSiblingWithCopiedFloat : PatchOperationPathedExtended
    {
        /// <summary>
        /// Adds or replaces a node to a targeted sibling with that siblings InnerText value.
        /// Optionally add/subtract/multiply/divide the value added to the source node.
        /// Created to add plants to biomes with specific plants, and with that plants commonality, but maybe other useful stuff too.
        /// </summary>

        private readonly string node = null;
        private readonly string operation = null;
        private readonly string value = null;

        protected override bool ApplyWorker(XmlDocument xml)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(node))
                    return false;

                if (!PreCheck(xpath, xml))
                    return false;

                bool modified = false;
                foreach (XmlNode targetNode in nodes)
                {
                    // Get the target node's InnerText (the one we're copying from)
                    string targetValue = targetNode.InnerText;
                    if (string.IsNullOrWhiteSpace(targetValue))
                        continue;

                    // Apply operation if specified
                    if (!string.IsNullOrWhiteSpace(operation) && !string.IsNullOrWhiteSpace(value))
                    {
                        targetValue = ApplyOperation(targetValue, operation, value);
                        if (targetValue == null)
                            continue; // Skip if operation failed
                    }

                    // Get the parent node where we'll add/replace the sibling
                    XmlNode parentNode = targetNode.ParentNode;
                    if (parentNode == null)
                        continue;

                    XmlNode existingNode = FindNodeByName(parentNode, node);

                    if (existingNode != null)
                    {
                        // Replace existing node's InnerText
                        existingNode.InnerText = targetValue;
                        modified = true;
                    }
                    else
                    {
                        // Add new node as sibling
                        XmlNode newNode = xml.CreateElement(node);
                        newNode.InnerText = targetValue;
                        parentNode.AppendChild(newNode);
                        modified = true;
                    }

                    ToLog($"xpath='{xpath}', node='{node}', value='{targetValue}'");
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