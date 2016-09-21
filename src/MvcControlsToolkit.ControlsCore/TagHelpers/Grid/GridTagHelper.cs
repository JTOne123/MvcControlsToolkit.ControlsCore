﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using MvcControlsToolkit.Core.OptionsParsing;
using MvcControlsToolkit.Core.TagHelpers.Internals;
using MvcControlsToolkit.Core.Templates;
using MvcControlsToolkit.Core.Views;

namespace MvcControlsToolkit.Core.TagHelpers
{
    public enum GridType {Batch, Immediate}
    [HtmlTargetElement(TagName, Attributes = ForAttributeName + "," + TypeAttributeName)]
    public class GridTagHelper : RowTypeTagHelper
    {
        private const string ForAttributeName = "asp-for";
        private const string TypeAttributeName = "type";
        private const string TagName = "grid";

        [HtmlAttributeName(TypeAttributeName)]
        public GridType Type { get; set; }
        [HtmlAttributeName("id")]
        public string OverrideId { get; set; }
        [HtmlAttributeName("class")]
        public string CssClass { get; set; }
        [HtmlAttributeName("server-row-selection")]
        public Func<object, int> ServerRowSelection { get; set; }
        [HtmlAttributeName("client-row-selection")]
        public string ClientRowSelection { get; set; }
        [HtmlAttributeName("layout-template")]
        public string LayoutTemplate { get; set; }
        [HtmlAttributeName("caption")]
        public string Caption { get; set; }
        [HtmlAttributeName("layout-parts")]
        public IEnumerable<string> LayoutParts { get; set; }

        [HtmlAttributeName("error-messages")]
        public GridErrorMessages ErrorMessages { get; set; }

        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }
        private string IdAttributeDotReplacement;
        private IHtmlHelper html;
        private IHttpContextAccessor httpAccessor;
        private IViewComponentHelper component;
        private IUrlHelperFactory urlHelperFactory;
        private IStringLocalizerFactory factory;
        public GridTagHelper(IOptions<MvcViewOptions> optionsAccessor,
            IHtmlHelper html, 
            IHttpContextAccessor httpAccessor, IViewComponentHelper component, 
            IUrlHelperFactory urlHelperFactory,
            IStringLocalizerFactory factory)
        {
            IdAttributeDotReplacement = optionsAccessor.Value.HtmlHelperOptions.IdAttributeDotReplacement;
            this.html = html;
            this.httpAccessor = httpAccessor;
            this.component = component;
            this.urlHelperFactory = urlHelperFactory;
            this.factory = factory;
        }
        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            //estabilish context
            if (For == null) throw new ArgumentNullException(ForAttributeName);
            string fullName = ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(For.Name);
            string id = OverrideId ?? TagBuilder.CreateSanitizedId(fullName, IdAttributeDotReplacement);
            var currProvider = ViewContext.TagHelperProvider();
            var defaultTemplates = currProvider.GetDefaultTemplates(TagName);
            var ctx = new ContextualizedHelpers(ViewContext, html, httpAccessor, component, urlHelperFactory, factory);
            //

            //get row definitions
            var nc = new ReductionContext(TagTokens.RowContainer, 0,defaultTemplates);
            context.SetChildrenReductionContext(nc);
            await output.GetChildContentAsync();
            var collector = new RowContainerCollector(nc);
            var res= collector.Process(this, defaultTemplates) as Tuple<IList<RowType>, IList<KeyValuePair<string, IHtmlContent>>>;
            var rows = res.Item1;
            var toolbars = res.Item2;
            //

            //Prepare grid options
            var options = new GridOptions(rows, toolbars, Type, id, fullName)
            {
                CssClass=CssClass,
                ErrorMessages=ErrorMessages,
                ClientRowSelection=ClientRowSelection,
                ServerRowSelection=ServerRowSelection,
                LayoutTemplate=defaultTemplates.GetLayoutTemplate(LayoutTemplate),
                SubTemplates=defaultTemplates.GetLayoutParts(LayoutParts)
            };
            //finally process!
            await currProvider.GetTagProcessor(TagName+(Type== GridType.Batch ? "-batch" : "-immediate"))(context, output, this, options, ctx);
        }
    }
}