﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Options;
using NetModular.Lib.Utils.Core.Extensions;
using NetModular.Lib.Utils.Core.Options;
using NetModular.Lib.Utils.Core.Result;
using NetModular.Module.CodeGenerator.Application.ProjectService.ResultModels;
using NetModular.Module.CodeGenerator.Application.ProjectService.ViewModels;
using NetModular.Module.CodeGenerator.Domain.Class;
using NetModular.Module.CodeGenerator.Domain.ClassMethod;
using NetModular.Module.CodeGenerator.Domain.Enum;
using NetModular.Module.CodeGenerator.Domain.EnumItem;
using NetModular.Module.CodeGenerator.Domain.ModelProperty;
using NetModular.Module.CodeGenerator.Domain.Project;
using NetModular.Module.CodeGenerator.Domain.Project.Models;
using NetModular.Module.CodeGenerator.Domain.Property;
using NetModular.Module.CodeGenerator.Infrastructure.Options;
using NetModular.Module.CodeGenerator.Infrastructure.Repositories;
using NetModular.Module.CodeGenerator.Infrastructure.Templates.Default;
using NetModular.Module.CodeGenerator.Infrastructure.Templates.Models;

namespace NetModular.Module.CodeGenerator.Application.ProjectService
{
    public class ProjectService : IProjectService
    {
        private readonly IMapper _mapper;
        private readonly ModuleCommonOptions _commonOptions;
        private readonly CodeGeneratorOptions _codeGeneratorOptions;
        private readonly IProjectRepository _repository;
        private readonly IClassRepository _classRepository;
        private readonly IPropertyRepository _propertyRepository;
        private readonly IEnumRepository _enumRepository;
        private readonly IEnumItemRepository _enumItemRepository;
        private readonly IModelPropertyRepository _modelPropertyRepository;
        private readonly IClassMethodRepository _classMethodRepository;
        private readonly CodeGeneratorDbContext _dbContext;

        public ProjectService(IProjectRepository repository, IMapper mapper, IOptionsMonitor<ModuleCommonOptions> optionsMonitor, IClassRepository classRepository, IPropertyRepository propertyRepository, IEnumRepository enumRepository, IEnumItemRepository enumItemRepository, IModelPropertyRepository modelPropertyRepository, IOptionsMonitor<CodeGeneratorOptions> codeGeneratorOptions, IClassMethodRepository classMethodRepository, CodeGeneratorDbContext dbContext)
        {
            _repository = repository;
            _mapper = mapper;
            _classRepository = classRepository;
            _propertyRepository = propertyRepository;
            _enumRepository = enumRepository;
            _enumItemRepository = enumItemRepository;
            _modelPropertyRepository = modelPropertyRepository;
            _classMethodRepository = classMethodRepository;
            _dbContext = dbContext;
            _codeGeneratorOptions = codeGeneratorOptions.CurrentValue;
            _commonOptions = optionsMonitor.CurrentValue;
        }

        public async Task<IResultModel> Query(ProjectQueryModel model)
        {
            var result = new QueryResultModel<ProjectEntity>
            {
                Rows = await _repository.Query(model),
                Total = model.TotalCount
            };
            return ResultModel.Success(result);
        }

        public async Task<IResultModel> Add(ProjectAddModel model)
        {
            var entity = _mapper.Map<ProjectEntity>(model);
            var result = await _repository.AddAsync(entity);
            return ResultModel.Result(result);
        }

        public async Task<IResultModel> Delete(Guid id)
        {
            var entity = await _repository.GetAsync(id);
            if (entity == null)
                return ResultModel.NotExists;

            using (var uow = _dbContext.NewUnitOfWork())
            {
                var result = await _repository.SoftDeleteAsync(id, uow);
                if (result)
                {
                    result = await _classRepository.DeleteByProject(id, uow);
                    if (result)
                    {
                        result = await _propertyRepository.DeleteByProject(id, uow);
                        if (result)
                        {
                            result = await _modelPropertyRepository.DeleteByProject(id, uow);
                            if (result)
                            {
                                uow.Commit();
                                return ResultModel.Success();
                            }
                        }
                    }
                }
            }

            return ResultModel.Failed();
        }

        public async Task<IResultModel> Edit(Guid id)
        {
            var entity = await _repository.GetAsync(id);
            if (entity == null)
                return ResultModel.NotExists;

            var model = _mapper.Map<ProjectUpdateModel>(entity);
            return ResultModel.Success(model);
        }

        public async Task<IResultModel> Update(ProjectUpdateModel model)
        {
            var entity = await _repository.GetAsync(model.Id);
            if (entity == null)
                return ResultModel.NotExists;

            _mapper.Map(model, entity);

            var result = await _repository.UpdateAsync(entity);

            return ResultModel.Result(result);
        }

        public Task<IResultModel<ProjectBuildCodeResultModel>> BuildCode(ProjectBuildCodeModel model)
        {
            return BuildCode(model.Id);
        }

        public async Task<IResultModel<ProjectBuildCodeResultModel>> BuildCode(Guid projectId, IList<ClassEntity> classList = null)
        {
            var result = new ResultModel<ProjectBuildCodeResultModel>();

            var project = await _repository.GetAsync(projectId);
            if (project == null)
                return result.Failed("项目不存在");

            //创建项目生成对象
            var projectBuildModel = _mapper.Map<ProjectBuildModel>(project);

            projectBuildModel.Prefix = _codeGeneratorOptions.Prefix;
            projectBuildModel.UIPrefix = _codeGeneratorOptions.UIPrefix;

            var id = Guid.NewGuid().ToString();
            var rootPath = Path.Combine(_commonOptions.TempPath, _codeGeneratorOptions.BuildCodePath);
            var buildModel = new TemplateBuildModel
            {
                RootPath = Path.Combine(rootPath, id),
            };

            if (classList == null)
            {
                classList = await _classRepository.QueryAllByProject(project.Id);
            }

            foreach (var classEntity in classList)
            {
                var classBuildModel = _mapper.Map<ClassBuildModel>(classEntity);
                var propertyList = await _propertyRepository.QueryByClass(classEntity.Id);
                if (propertyList.Any())
                {
                    //查询属性
                    foreach (var propertyEntity in propertyList)
                    {
                        var propertyBuildModel = _mapper.Map<PropertyBuildModel>(propertyEntity);

                        //如果属性类型是枚举，查询枚举信息
                        if (propertyEntity.Type == PropertyType.Enum && propertyEntity.EnumId.NotEmpty())
                        {
                            var enumEntity = await _enumRepository.GetAsync(propertyEntity.EnumId);
                            propertyBuildModel.Enum = new EnumBuildModel
                            {
                                Name = enumEntity.Name,
                                Remarks = enumEntity.Remarks
                            };

                            var enumItemList = await _enumItemRepository.QueryByEnum(propertyEntity.EnumId);
                            propertyBuildModel.Enum.ItemList = enumItemList.Select(m => new EnumItemBuildModel
                            {
                                Name = m.Name,
                                Remarks = m.Remarks,
                                Value = m.Value
                            }).ToList();
                        }

                        classBuildModel.PropertyList.Add(propertyBuildModel);
                    }
                }

                var modelPropertyList = await _modelPropertyRepository.QueryByClass(classEntity.Id);

                if (modelPropertyList.Any())
                {
                    foreach (var propertyEntity in modelPropertyList)
                    {
                        var modelPropertyBuildModel = _mapper.Map<ModelPropertyBuildModel>(propertyEntity);
                        //如果属性类型是枚举，查询枚举信息
                        if (propertyEntity.Type == PropertyType.Enum && propertyEntity.EnumId.NotEmpty())
                        {
                            var enumEntity = await _enumRepository.GetAsync(propertyEntity.EnumId);
                            modelPropertyBuildModel.Enum = new EnumBuildModel
                            {
                                Name = enumEntity.Name,
                                Remarks = enumEntity.Remarks
                            };

                            var enumItemList = await _enumItemRepository.QueryByEnum(propertyEntity.EnumId);
                            modelPropertyBuildModel.Enum.ItemList = enumItemList.Select(m => new EnumItemBuildModel
                            {
                                Name = m.Name,
                                Remarks = m.Remarks,
                                Value = m.Value
                            }).ToList();
                        }

                        classBuildModel.ModelPropertyList.Add(modelPropertyBuildModel);
                    }
                }

                classBuildModel.Method = await _classMethodRepository.GetByClass(classEntity.Id);

                projectBuildModel.ClassList.Add(classBuildModel);
            }

            buildModel.Project = projectBuildModel;

            var builder = new DefaultTemplateBuilder();
            builder.Build(buildModel);

            ZipFile.CreateFromDirectory(buildModel.RootPath, Path.Combine(rootPath, id + ".zip"));

            var resultModel = new ProjectBuildCodeResultModel
            {
                Id = id,
                Name = projectBuildModel.Name + ".zip"
            };

            return result.Success(resultModel);
        }
    }
}
