#pragma checksum "G:\Projects\SmartMining\MCS\MCSWebApp\Areas\Material\Views\ProductSpecification\Detail.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "942df09e5ab8ebd64fb2d79f5a61faad01b82c6c"
// <auto-generated/>
#pragma warning disable 1591
[assembly: global::Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemAttribute(typeof(AspNetCore.Areas_Material_Views_ProductSpecification_Detail), @"mvc.1.0.view", @"/Areas/Material/Views/ProductSpecification/Detail.cshtml")]
namespace AspNetCore
{
    #line hidden
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
#nullable restore
#line 1 "G:\Projects\SmartMining\MCS\MCSWebApp\Areas\Material\Views\_ViewImports.cshtml"
using MCSWebApp;

#line default
#line hidden
#nullable disable
#nullable restore
#line 2 "G:\Projects\SmartMining\MCS\MCSWebApp\Areas\Material\Views\_ViewImports.cshtml"
using MCSWebApp.Models;

#line default
#line hidden
#nullable disable
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA1", @"942df09e5ab8ebd64fb2d79f5a61faad01b82c6c", @"/Areas/Material/Views/ProductSpecification/Detail.cshtml")]
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA1", @"f0bac7701d80dd640522d03d9f8d7561ca165a12", @"/Areas/Material/Views/_ViewImports.cshtml")]
    public class Areas_Material_Views_ProductSpecification_Detail : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<dynamic>
    {
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_0 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("id", new global::Microsoft.AspNetCore.Html.HtmlString("form-main"), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        #line hidden
        #pragma warning disable 0649
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperExecutionContext __tagHelperExecutionContext;
        #pragma warning restore 0649
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner __tagHelperRunner = new global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner();
        #pragma warning disable 0169
        private string __tagHelperStringValueBuffer;
        #pragma warning restore 0169
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager __backed__tagHelperScopeManager = null;
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager __tagHelperScopeManager
        {
            get
            {
                if (__backed__tagHelperScopeManager == null)
                {
                    __backed__tagHelperScopeManager = new global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager(StartTagHelperWritingScope, EndTagHelperWritingScope);
                }
                return __backed__tagHelperScopeManager;
            }
        }
        private global::Microsoft.AspNetCore.Mvc.TagHelpers.FormTagHelper __Microsoft_AspNetCore_Mvc_TagHelpers_FormTagHelper;
        private global::Microsoft.AspNetCore.Mvc.TagHelpers.RenderAtEndOfFormTagHelper __Microsoft_AspNetCore_Mvc_TagHelpers_RenderAtEndOfFormTagHelper;
        private global::Microsoft.AspNetCore.Mvc.Razor.TagHelpers.UrlResolutionTagHelper __Microsoft_AspNetCore_Mvc_Razor_TagHelpers_UrlResolutionTagHelper;
        #pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
            WriteLiteral("<div class=\"row\">\r\n    <div class=\"col-xl-12\">\r\n        <div id=\"panel-1\" class=\"panel\">\r\n            <div class=\"panel-hdr\">\r\n                <h2>\r\n                    ");
#nullable restore
#line 6 "G:\Projects\SmartMining\MCS\MCSWebApp\Areas\Material\Views\ProductSpecification\Detail.cshtml"
               Write(ViewBag.Breadcrumb);

#line default
#line hidden
#nullable disable
            WriteLiteral(@"
                </h2>
                <div class=""panel-toolbar ml-2"">
                    <button type=""button"" id=""Save"" name=""Save"" class=""btn btn-sm btn-primary"">Save</button>
                </div>
                <div class=""panel-toolbar ml-2"">
                    <button type=""button"" id=""Delete"" name=""Delete"" class=""btn btn-sm btn-danger"">Delete</button>
                </div>
                <div class=""panel-toolbar ml-2"">
                    <button class=""btn btn-panel"" data-action=""panel-collapse"" data-toggle=""tooltip"" data-offset=""0,10"" data-original-title=""Collapse""></button>
                    <button class=""btn btn-panel"" data-action=""panel-fullscreen"" data-toggle=""tooltip"" data-offset=""0,10"" data-original-title=""Fullscreen""></button>
                </div>
            </div>
            <div class=""panel-container show"">
                <div class=""panel-content"">
                    <div class=""row"">
                        <div class=""col-xl-12"">
                       ");
            WriteLiteral("     ");
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("form", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "942df09e5ab8ebd64fb2d79f5a61faad01b82c6c5414", async() => {
                WriteLiteral("\r\n                                <input type=\"hidden\" id=\"id\" name=\"id\"");
                BeginWriteAttribute("value", " value=\"", 1310, "\"", 1329, 1);
#nullable restore
#line 24 "G:\Projects\SmartMining\MCS\MCSWebApp\Areas\Material\Views\ProductSpecification\Detail.cshtml"
WriteAttributeValue("", 1318, ViewBag.Id, 1318, 11, false);

#line default
#line hidden
#nullable disable
                EndWriteAttribute();
                WriteLiteral(@" />
                                <input type=""hidden"" id=""product_id"" name=""product_id"" />
                                <input type=""hidden"" id=""product_name"" />
                                <input type=""hidden"" id=""analyte_id"" name=""analyte_id"" />
                                <input type=""hidden"" id=""analyte_name"" />
                                <input type=""hidden"" id=""uom_id"" name=""uom_id"" />
                                <input type=""hidden"" id=""uom_name"" />

                                <div class=""form-group"">
                                    <label class=""form-label"" for=""Product"">
                                        Product
                                    </label>
                                    <select data-placeholder=""Product ..."" class=""form-control"" id=""Product""></select>
                                </div>

                                <div class=""row form-group"">
                                    <div class=""col-md-6"">
                ");
                WriteLiteral(@"                        <div class=""form-group"">
                                            <label class=""form-label"" for=""Analyte"">
                                                Analyte
                                            </label>
                                            <select data-placeholder=""Analyte ..."" class=""form-control"" id=""Analyte""></select>
                                        </div>
                                    </div>
                                    <div class=""col-md-6"">
                                        <div class=""form-group"">
                                            <label class=""form-label"" for=""UOM"">
                                                UOM
                                            </label>
                                            <select data-placeholder=""UOM ..."" class=""form-control"" id=""UOM""></select>
                                        </div>
                                    </div>
                              ");
                WriteLiteral(@"  </div>

                                <div class=""row form-group"">
                                    <div class=""col-md-6"">
                                        <div class=""form-group"">
                                            <label class=""form-label"" for=""target_value"">Target Value</label>
                                            <input type=""text"" id=""target_value"" name=""target_value"" class=""form-control"">
                                        </div>
                                    </div>
                                    <div class=""col-md-6"">
                                        <div class=""form-group"">
                                            <label class=""form-label"" for=""applicable_date"">Applicable Date</label>
                                            <input type=""datetime-local"" id=""applicable_date"" name=""applicable_date"" class=""form-control"">
                                        </div>
                                    </div>
                      ");
                WriteLiteral(@"          </div>

                                <div class=""row form-group"">
                                    <div class=""col-md-6"">
                                        <div class=""form-group"">
                                            <label class=""form-label"" for=""minimum_value"">Minimum Value</label>
                                            <input type=""text"" id=""minimum_value"" name=""minimum_value"" class=""form-control"">
                                        </div>
                                    </div>
                                    <div class=""col-md-6"">
                                        <div class=""form-group"">
                                            <label class=""form-label"" for=""maximum_value"">Maximum Value</label>
                                            <input type=""text"" id=""maximum_value"" name=""maximum_value"" class=""form-control"">
                                        </div>
                                    </div>
                            ");
                WriteLiteral("    </div>\r\n\r\n                            ");
            }
            );
            __Microsoft_AspNetCore_Mvc_TagHelpers_FormTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.TagHelpers.FormTagHelper>();
            __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_TagHelpers_FormTagHelper);
            __Microsoft_AspNetCore_Mvc_TagHelpers_RenderAtEndOfFormTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.TagHelpers.RenderAtEndOfFormTagHelper>();
            __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_TagHelpers_RenderAtEndOfFormTagHelper);
            __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_0);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Write(__tagHelperExecutionContext.Output);
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            WriteLiteral("\r\n                        </div>\r\n                    </div>\r\n                </div>\r\n            </div>\r\n        </div>\r\n    </div>\r\n</div>\r\n\r\n");
            DefineSection("ScriptsBlock", async() => {
                WriteLiteral("\r\n    ");
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("script", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "942df09e5ab8ebd64fb2d79f5a61faad01b82c6c11881", async() => {
                }
                );
                __Microsoft_AspNetCore_Mvc_Razor_TagHelpers_UrlResolutionTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.Razor.TagHelpers.UrlResolutionTagHelper>();
                __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_Razor_TagHelpers_UrlResolutionTagHelper);
                BeginWriteTagHelperAttribute();
                __tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
                __tagHelperExecutionContext.AddHtmlAttribute("defer", Html.Raw(__tagHelperStringValueBuffer), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.Minimized);
                BeginAddHtmlAttributeValues(__tagHelperExecutionContext, "src", 2, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
                AddHtmlAttributeValue("", 5668, "~/app/areas/material/productspecification/js/detail.js?_=", 5668, 57, true);
#nullable restore
#line 99 "G:\Projects\SmartMining\MCS\MCSWebApp\Areas\Material\Views\ProductSpecification\Detail.cshtml"
AddHtmlAttributeValue("", 5725, DateTime.Now.Ticks, 5725, 19, false);

#line default
#line hidden
#nullable disable
                EndAddHtmlAttributeValues(__tagHelperExecutionContext);
                await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
                if (!__tagHelperExecutionContext.Output.IsContentModified)
                {
                    await __tagHelperExecutionContext.SetOutputContentAsync();
                }
                Write(__tagHelperExecutionContext.Output);
                __tagHelperExecutionContext = __tagHelperScopeManager.End();
                WriteLiteral("\r\n");
            }
            );
        }
        #pragma warning restore 1998
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.ViewFeatures.IModelExpressionProvider ModelExpressionProvider { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IUrlHelper Url { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IViewComponentHelper Component { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper Json { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<dynamic> Html { get; private set; }
    }
}
#pragma warning restore 1591
