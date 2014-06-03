using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.UI.WebControls;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace IsobarExtension.Actions
{
    public sealed class XmlConfigTransformActionEditor : ActionEditorBase
    {
        private ValidatingTextBox _environmentSuffix;
        private ValidatingTextBox _additionalTransforms;
        private CheckBox _preservceWhiteSpace;
        private CheckBox _verbose;

        public override void BindToForm(Inedo.BuildMaster.Extensibility.Actions.ActionBase extension)
        {
            var action = (XmlConfigTransformAction)extension;
            _environmentSuffix.Text = action.EnvironmentSuffix;
            _additionalTransforms.Text = action.AdditionalTransforms;
            _preservceWhiteSpace.Checked = action.PreserveWhitespace;
            _verbose.Checked = action.Verbose;
        }

        public override Inedo.BuildMaster.Extensibility.Actions.ActionBase CreateFromForm()
        {
            return new XmlConfigTransformAction()
                {
                    EnvironmentSuffix = _environmentSuffix.Text,
                    AdditionalTransforms = _additionalTransforms.Text,
                    PreserveWhitespace = _preservceWhiteSpace.Checked,
                    Verbose = _verbose.Checked
                };
        }

        protected override void CreateChildControls()
        {
            _environmentSuffix = new ValidatingTextBox { Width = 300, Required = true };
            _additionalTransforms = new ValidatingTextBox { Width = 300, Required = false };
            _preservceWhiteSpace = new CheckBox { Text = "Preserve Whitespace in Destination File", Checked = true };
            _verbose = new CheckBox { Text = "Enable Verbose Logging", Checked = true };

            Controls.Add(
                new FormFieldGroup(
                    "Environment Suffix",
                    "By default, Build Master looks for *.Release.config. Enter the environment suffix to look for *.<environment-name>.config.",
                    false,
                    new StandardFormField("Environment Suffix", _environmentSuffix)
                    ),
                new FormFieldGroup(
                    "Additional Transforms",
                    "A comma-separated list of additional configuration transformation file suffixes to run if found. E.g., Deploy.config,Test.config. Note that these should be suffixes, so for example, if your configuration file is named Web.config, and your transformation file is named Web.Production.config, you should enter Production.config.",
                    false,
                    new StandardFormField("Additional Transforms:", _additionalTransforms)
                    ),
                new FormFieldGroup(
                    "Additional Options",
                    "Specify whether whitespace should be preserved in the destination file, and if verbose logging should be captured.",
                    false,
                    new StandardFormField("", _preservceWhiteSpace),
                    new StandardFormField("", _verbose)
                    )
                );
        }
    }
}
