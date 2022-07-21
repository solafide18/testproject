#pragma checksum "G:\Projects\SmartMining\MCS\MCSWebApp\Areas\Authentication\Views\Login\ForgotPassword.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "821d327e9b0e94e0f04e03bb140e9b365fd8f6e7"
// <auto-generated/>
#pragma warning disable 1591
[assembly: global::Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemAttribute(typeof(AspNetCore.Areas_Authentication_Views_Login_ForgotPassword), @"mvc.1.0.view", @"/Areas/Authentication/Views/Login/ForgotPassword.cshtml")]
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
#line 1 "G:\Projects\SmartMining\MCS\MCSWebApp\Areas\Authentication\Views\_ViewImports.cshtml"
using MCSWebApp;

#line default
#line hidden
#nullable disable
#nullable restore
#line 2 "G:\Projects\SmartMining\MCS\MCSWebApp\Areas\Authentication\Views\_ViewImports.cshtml"
using MCSWebApp.Models;

#line default
#line hidden
#nullable disable
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA1", @"821d327e9b0e94e0f04e03bb140e9b365fd8f6e7", @"/Areas/Authentication/Views/Login/ForgotPassword.cshtml")]
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA1", @"b7186d60004db705060fd1b00f51aade056042ba", @"/Areas/Authentication/Views/_ViewImports.cshtml")]
    public class Areas_Authentication_Views_Login_ForgotPassword : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<dynamic>
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
            WriteLiteral(@"<div class=""flex-1"" style=""background: url(img/svg/pattern-1.svg) no-repeat center bottom fixed; background-size: cover;"">
    <div class=""container py-4 py-lg-5 my-lg-5 px-4 px-sm-0"">
        <div class=""row"">
            <div class=""col-xl-12"">
                <h2 class=""fs-xxl fw-500 mt-4 text-white text-center"">
                    Forgot Password
                    <small class=""h3 fw-300 mt-3 mb-5 text-white opacity-60 hidden-sm-down"">
                        For security purpose, this request may need approval from system administrator
                    </small>
                </h2>
            </div>
            <div class=""col-xl-6 ml-auto mr-auto"">
                <div class=""card p-4 rounded-plus bg-faded"">
                    ");
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("form", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "821d327e9b0e94e0f04e03bb140e9b365fd8f6e74663", async() => {
                WriteLiteral("\r\n                        <div class=\"form-group\">\r\n                            <label class=\"form-label\" for=\"lostaccount\">Your username or email</label>\r\n                            <input type=\"email\" id=\"lostaccount\" class=\"form-control\" required");
                BeginWriteAttribute("value", " value=\"", 1036, "\"", 1044, 0);
                EndWriteAttribute();
                WriteLiteral(@">
                            <div class=""invalid-feedback"">No, you missed this one.</div>
                            <div class=""help-block"">We will email you the instructions</div>
                        </div>
                        <div class=""row no-gutters"">
                            <div class=""col-md-4 ml-auto text-right"">
                                <button id=""btn-forgot-password"" type=""button"" class=""btn btn-danger"">Submit</button>
                            </div>
                        </div>
                    ");
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
            WriteLiteral("\r\n                </div>\r\n            </div>\r\n        </div>\r\n    </div>\r\n    <div class=\"d-block text-center text-white\">\r\n        ");
#nullable restore
#line 32 "G:\Projects\SmartMining\MCS\MCSWebApp\Areas\Authentication\Views\Login\ForgotPassword.cshtml"
   Write(DateTime.Now.Year);

#line default
#line hidden
#nullable disable
            WriteLiteral(" © Smart Mining by&nbsp;<a href=\'https://www.adiraja-integrasi.com\' class=\'text-white opacity-40 fw-500\' title=\'www.adiraja-integrasi.com\' target=\'_blank\'>Adiraja Integrasi</a>\r\n    </div>\r\n</div>\r\n");
            DefineSection("ScriptsBlock", async() => {
                WriteLiteral("\r\n    ");
                __tagHelperExecutionContext = __tagHelperScopeManager.Begin("script", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "821d327e9b0e94e0f04e03bb140e9b365fd8f6e77653", async() => {
                }
                );
                __Microsoft_AspNetCore_Mvc_Razor_TagHelpers_UrlResolutionTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.Razor.TagHelpers.UrlResolutionTagHelper>();
                __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_Razor_TagHelpers_UrlResolutionTagHelper);
                BeginWriteTagHelperAttribute();
                __tagHelperStringValueBuffer = EndWriteTagHelperAttribute();
                __tagHelperExecutionContext.AddHtmlAttribute("defer", Html.Raw(__tagHelperStringValueBuffer), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.Minimized);
                BeginAddHtmlAttributeValues(__tagHelperExecutionContext, "src", 2, global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
                AddHtmlAttributeValue("", 2000, "~/app/areas/authentication/login/js/forgot_password.js?_=", 2000, 57, true);
#nullable restore
#line 37 "G:\Projects\SmartMining\MCS\MCSWebApp\Areas\Authentication\Views\Login\ForgotPassword.cshtml"
AddHtmlAttributeValue("", 2057, DateTime.Now.Ticks, 2057, 19, false);

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
