﻿using System.Collections.Generic;
using System.Linq;
using NetModular.Lib.Utils.Core.Extensions;
using NetModular.Module.CodeGenerator.Domain.Class;
using NetModular.Module.CodeGenerator.Domain.ClassMethod;
using NetModular.Module.CodeGenerator.Domain.ModelProperty;

namespace NetModular.Module.CodeGenerator.Infrastructure.Templates.Models
{
    /// <summary>
    /// 类生成模型
    /// </summary>
    public class ClassBuildModel
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 表名称
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// 基类类型
        /// </summary>
        public BaseEntityType BaseEntityType { get; set; }

        /// <summary>
        /// 说明
        /// </summary>
        public string Remarks { get; set; }

        /// <summary>
        /// 主键类型名称
        /// </summary>
        public string PrimaryKeyTypeName
        {
            get
            {
                var name = BaseEntityType.ToDescription().ToLower();
                if (name.Contains("int")) return "int";
                if (name.Contains("long")) return "long";
                if (name.Contains("guid")) return "Guid";
                return "string";
            }
        }

        /// <summary>
        /// 属性列表
        /// </summary>
        public List<PropertyBuildModel> PropertyList { get; set; } = new List<PropertyBuildModel>();

        /// <summary>
        /// 模型属性列表
        /// </summary>
        public List<ModelPropertyBuildModel> ModelPropertyList { get; set; } = new List<ModelPropertyBuildModel>();

        /// <summary>
        /// 是否是EntityBase类型的实体
        /// </summary>
        public bool IsEntityBase => BaseEntityType.ToDescription().StartsWith("EntityBase");

        /// <summary>
        /// 是否包含软删除
        /// </summary>
        public bool IsSoftDelete => BaseEntityType.ToDescription().StartsWith("SoftDelete");

        /// <summary>
        /// 查询模型属性列表
        /// </summary>
        public List<ModelPropertyBuildModel> QueryModelPropertyList
        {
            get
            {
                List<ModelPropertyBuildModel> list = null;
                if (ModelPropertyList.Any())
                {
                    list = ModelPropertyList.Where(m => m.ModelType == ModelType.Query).OrderBy(m => m.Sort).ToList();
                }

                if (list == null || !list.Any())
                {
                    list = PropertyList.OrderBy(m => m.Sort).Where(m => !m.IsInherit).Select(m => new ModelPropertyBuildModel
                    {
                        Name = m.Name,
                        Enum = m.Enum,
                        Nullable = m.Nullable,
                        Sort = m.Sort,
                        Remarks = m.Remarks,
                        Type = m.Type
                    }).ToList();
                }

                return list;
            }
        }

        /// <summary>
        /// 新增模型属性列表
        /// </summary>
        public List<ModelPropertyBuildModel> AddModelPropertyList
        {
            get
            {
                List<ModelPropertyBuildModel> list = null;
                if (ModelPropertyList.Any())
                {
                    list = ModelPropertyList.Where(m => m.ModelType == ModelType.Add).OrderBy(m => m.Sort).ToList();
                }

                if (list == null || !list.Any())
                {
                    list = PropertyList.OrderBy(m => m.Sort).Where(m => !m.IsInherit).Select(m => new ModelPropertyBuildModel
                    {
                        Name = m.Name,
                        Enum = m.Enum,
                        Nullable = m.Nullable,
                        Sort = m.Sort,
                        Remarks = m.Remarks,
                        Type = m.Type
                    }).ToList();
                }

                return list;
            }
        }

        /// <summary>
        /// 编辑模型属性列表
        /// </summary>
        public List<ModelPropertyBuildModel> EditModelPropertyList
        {
            get
            {
                List<ModelPropertyBuildModel> list = null;
                if (ModelPropertyList.Any())
                {
                    list = ModelPropertyList.Where(m => m.ModelType == ModelType.Edit).OrderBy(m => m.Sort).ToList();
                }

                if (list == null || !list.Any())
                {
                    list = PropertyList.OrderBy(m => m.Sort).Where(m => !m.IsInherit).Select(m => new ModelPropertyBuildModel
                    {
                        Name = m.Name,
                        Enum = m.Enum,
                        Nullable = m.Nullable,
                        Sort = m.Sort,
                        Remarks = m.Remarks,
                        Type = m.Type
                    }).ToList();
                }

                return list;
            }
        }

        /// <summary>
        /// 方法
        /// </summary>
        public ClassMethodEntity Method { get; set; }
    }
}
