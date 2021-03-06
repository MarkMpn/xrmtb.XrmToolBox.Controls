﻿using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata.Query;
using Microsoft.Xrm.Sdk.Messages;
using System.Linq;

namespace xrmtb.XrmToolBox.Controls
{
    public static class MetadataHelper
    {
        private static Dictionary<string, EntityMetadata> entities = new Dictionary<string, EntityMetadata>();

        public static String[] entityProperties = { "LogicalName", "DisplayName", "ObjectTypeCode", "IsManaged", "IsCustomizable", "IsCustomEntity", "IsIntersect", "IsValidForAdvancedFind" };
        public static String[] entityDetails = { "Attributes", "ManyToOneRelationships", "OneToManyRelationships", "ManyToManyRelationships", "SchemaName", "LogicalCollectionName", "PrimaryIdAttribute" };
        public static String[] attributeProperties = { "DisplayName", "AttributeType", "IsValidForRead", "AttributeOf", "IsManaged", "IsCustomizable", "IsCustomAttribute", "IsValidForAdvancedFind", "IsPrimaryId", "IsPrimaryName", "OptionSet", "SchemaName", "Targets" };

        public static AttributeMetadata GetAttribute(IOrganizationService service, string entity, string attribute, object value)
        {
            if (value is AliasedValue)
            {
                var aliasedValue = value as AliasedValue;
                entity = aliasedValue.EntityLogicalName;
                attribute = aliasedValue.AttributeLogicalName;
            }
            return GetAttribute(service, entity, attribute);
        }

        public static AttributeMetadata GetAttribute(IOrganizationService service, string entity, string attribute)
        {
            var entitymeta = GetEntity(service, entity);
            if (entitymeta != null)
            {
                if (entitymeta.Attributes != null)
                {
                    foreach (var metaattribute in entitymeta.Attributes)
                    {
                        if (metaattribute.LogicalName == attribute)
                        {
                            return metaattribute;
                        }
                    }
                }
            }
            return null;
        }

        private static EntityMetadata GetEntity(IOrganizationService service, string entity)
        {
            if (!entities.ContainsKey(entity))
            {
                var response = LoadEntityDetails(service, entity);
                if (response != null && response.EntityMetadata != null && response.EntityMetadata.Count == 1 && response.EntityMetadata[0].LogicalName == entity)
                {
                    entities.Add(entity, response.EntityMetadata[0]);
                }
            }
            return entities[entity];
        }

        public static AttributeMetadata GetPrimaryAttribute(IOrganizationService service, string entity)
        {
            var entitymeta = GetEntity(service, entity);
            if (entitymeta != null)
            {
                if (entitymeta.Attributes != null)
                {
                    foreach (var metaattribute in entitymeta.Attributes)
                    {
                        if (metaattribute.IsPrimaryName == true)
                        {
                            return metaattribute;
                        }
                    }
                }
            }
            return null;
        }

        public static RetrieveMetadataChangesResponse LoadEntities(IOrganizationService service)
        {
            if (service == null)
            {
                return null;
            }
            var eqe = new EntityQueryExpression();
            eqe.Properties = new MetadataPropertiesExpression(entityProperties);
            var req = new RetrieveMetadataChangesRequest()
            {
                Query = eqe,
                ClientVersionStamp = null
            };
            return service.Execute(req) as RetrieveMetadataChangesResponse;
        }

        public static RetrieveMetadataChangesResponse LoadEntityDetails(IOrganizationService service, string entityName, int orgMajorVer = 0, int orgMinorVer = 0)
        {
            if (service == null)
            {
                return null;
            }
            var eqe = new EntityQueryExpression();
            eqe.Properties = new MetadataPropertiesExpression(entityProperties);
            string[] details = GetEntityDetailsForVersion(orgMajorVer, orgMinorVer);
            eqe.Properties.PropertyNames.AddRange(details);
            eqe.Criteria.Conditions.Add(new MetadataConditionExpression("LogicalName", MetadataConditionOperator.Equals, entityName));
            var aqe = new AttributeQueryExpression();
            aqe.Properties = new MetadataPropertiesExpression(attributeProperties);
            eqe.AttributeQuery = aqe;
            var req = new RetrieveMetadataChangesRequest()
            {
                Query = eqe,
                ClientVersionStamp = null
            };
            return service.Execute(req) as RetrieveMetadataChangesResponse;
        }

        private static string[] GetEntityDetailsForVersion(int orgMajorVer, int orgMinorVer)
        {
            var result = entityDetails.ToList();
            if (orgMajorVer < 8)
            {
                result.Remove("LogicalCollectionName");
            }
            return result.ToArray();
        }
    }
}

