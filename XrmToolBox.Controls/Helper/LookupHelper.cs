using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;

namespace xrmtb.XrmToolBox.Controls.Helper
{
    static class LookupHelper
    {
        public static EntityCollection ExecuteQuickFind(IOrganizationService service, string logicalName, string search)
        {
            var qe = new QueryExpression("savedquery");
            qe.ColumnSet = new ColumnSet("fetchxml");
            qe.Criteria.AddCondition("returnedtypecode", ConditionOperator.Equal, logicalName);
            qe.Criteria.AddCondition("querytype", ConditionOperator.Equal, 4);
            qe.Criteria.AddCondition("isdefault", ConditionOperator.Equal, true);
            var views = service.RetrieveMultiple(qe);

            if (views.Entities.Count == 0)
            {
                throw new ApplicationException("Unable to find Quick Find view for entity " + logicalName);
            }

            return ExecuteQuickFind(service, logicalName, views.Entities[0], search);
        }

        public static EntityCollection ExecuteQuickFind(IOrganizationService service, string logicalName, Entity view, string search)
        {
            var fetchDoc = new XmlDocument();
            fetchDoc.LoadXml(view.GetAttributeValue<string>("fetchxml"));
            var filterNodes = fetchDoc.SelectNodes("fetch/entity/filter");
            var metadata = MetadataHelper.LoadEntityDetails(service, logicalName).EntityMetadata[0];
            foreach (XmlNode filterNode in filterNodes)
            {
                ProcessFilter(metadata, filterNode, search);
            }

            return service.RetrieveMultiple(new FetchExpression { Query = fetchDoc.OuterXml });
        }

        private static void ProcessFilter(EntityMetadata metadata, XmlNode node, string searchTerm)
        {
            foreach (XmlNode condition in node.SelectNodes("condition"))
            {
                if (!condition.Attributes["value"].Value.StartsWith("{"))
                {
                    continue;
                }
                var attr = metadata.Attributes.First(a => a.LogicalName == condition.Attributes["attribute"].Value);

                #region Manage each attribute type

                switch (attr.AttributeType.Value)
                {
                    case AttributeTypeCode.Memo:
                    case AttributeTypeCode.String:
                        {
                            condition.Attributes["value"].Value = searchTerm.Replace("*", "%") + "%";
                        }
                        break;
                    case AttributeTypeCode.Boolean:
                        {
                            if (searchTerm != "0" && searchTerm != "1")
                            {
                                node.RemoveChild(condition);
                                continue;
                            }

                            condition.Attributes["value"].Value = (searchTerm == "1").ToString();
                        }
                        break;
                    case AttributeTypeCode.Customer:
                    case AttributeTypeCode.Lookup:
                    case AttributeTypeCode.Owner:
                        {
                            if (
                                metadata.Attributes.FirstOrDefault(
                                    a => a.LogicalName == condition.Attributes["attribute"].Value + "name") == null)
                            {
                                node.RemoveChild(condition);

                                continue;
                            }


                            condition.Attributes["attribute"].Value += "name";
                            condition.Attributes["value"].Value = searchTerm.Replace("*", "%") + "%";
                        }
                        break;
                    case AttributeTypeCode.DateTime:
                        {
                            DateTime dt;
                            if (!DateTime.TryParse(searchTerm, out dt))
                            {
                                condition.Attributes["value"].Value = new DateTime(1754, 1, 1).ToString("yyyy-MM-dd");
                            }
                            else
                            {
                                condition.Attributes["value"].Value = dt.ToString("yyyy-MM-dd");
                            }
                        }
                        break;
                    case AttributeTypeCode.Decimal:
                    case AttributeTypeCode.Double:
                    case AttributeTypeCode.Money:
                        {
                            decimal d;
                            if (!decimal.TryParse(searchTerm, out d))
                            {
                                condition.Attributes["value"].Value = int.MinValue.ToString(CultureInfo.InvariantCulture);
                            }
                            else
                            {
                                condition.Attributes["value"].Value = d.ToString(CultureInfo.InvariantCulture);
                            }
                        }
                        break;
                    case AttributeTypeCode.Integer:
                        {
                            int d;
                            if (!int.TryParse(searchTerm, out d))
                            {
                                condition.Attributes["value"].Value = int.MinValue.ToString(CultureInfo.InvariantCulture);
                            }
                            else
                            {
                                condition.Attributes["value"].Value = d.ToString(CultureInfo.InvariantCulture);
                            }
                        }
                        break;
                    case AttributeTypeCode.Picklist:
                        {
                            var opt = ((PicklistAttributeMetadata)attr).OptionSet.Options.FirstOrDefault(
                                o => o.Label.UserLocalizedLabel.Label == searchTerm);

                            if (opt == null)
                            {
                                condition.Attributes["value"].Value = int.MinValue.ToString(CultureInfo.InvariantCulture);
                            }
                            else
                            {
                                condition.Attributes["value"].Value = opt.Value.Value.ToString(CultureInfo.InvariantCulture);
                            }
                        }
                        break;
                    case AttributeTypeCode.State:
                        {
                            var opt = ((StateAttributeMetadata)attr).OptionSet.Options.FirstOrDefault(
                                o => o.Label.UserLocalizedLabel.Label == searchTerm);

                            if (opt == null)
                            {
                                condition.Attributes["value"].Value = int.MinValue.ToString(CultureInfo.InvariantCulture);
                            }
                            else
                            {
                                condition.Attributes["value"].Value = opt.Value.Value.ToString(CultureInfo.InvariantCulture);
                            }
                        }
                        break;
                    case AttributeTypeCode.Status:
                        {
                            var opt = ((StatusAttributeMetadata)attr).OptionSet.Options.FirstOrDefault(
                                o => o.Label.UserLocalizedLabel.Label == searchTerm);

                            if (opt == null)
                            {
                                condition.Attributes["value"].Value = int.MinValue.ToString(CultureInfo.InvariantCulture);
                            }
                            else
                            {
                                condition.Attributes["value"].Value = opt.Value.Value.ToString(CultureInfo.InvariantCulture);
                            }
                        }
                        break;
                }

                #endregion
            }

            foreach (XmlNode filter in node.SelectNodes("filter"))
            {
                ProcessFilter(metadata, filter, searchTerm);
            }
        }
    }
}
