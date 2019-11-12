﻿using System.IO;
using NetModular.Module.CodeGenerator.Infrastructure.Templates.Models;

namespace NetModular.Module.CodeGenerator.Infrastructure.Templates.Default.T4.src.UI.App.src.components
{
    public partial class Components : ITemplateHandler
    {
        private readonly TemplateBuildModel _model;
        private readonly string _prefix;
        private readonly string _uiPrefix;

        public Components(TemplateBuildModel model)
        {
            _model = model;
            _prefix = _model.Project.Prefix.ToLower();
            _uiPrefix = _model.Project.UIPrefix.ToLower();
        }

        public void Save()
        {
            var dir = Path.Combine(_model.RootPath, _model.Project.Code, $"src/UI/{_model.Project.WebUIDicName}/src/components");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var content = TransformText();
            var filePath = Path.Combine(dir, "index.js");
            File.WriteAllText(filePath, content);
        }
    }
}
