﻿using System;

using Azos.Web;
using Azos.Wave.Mvc;

namespace Azos.Tests.Unit.Wave.Controllers
{
  public class Basic : Controller
  {
    [Action]
    public object ActionPlainText() => "Response in plain text";

    [Action]
    public object ActionObjectLiteral() => new {a = 1, b = true, d = new DateTime(1980, 1, 1)};

    [Action]
    public void ActionHardCodedHtml()
    {
      WorkContext.Response.ContentType = ContentType.HTML;
      WorkContext.Response.Write("<h1>Hello HTML</h1>");
    }

    [Action(Name = "pmatch", Order = 10000)]//the last one - catch all
    public object PatternMatch_1_Any() => "any";

    [ActionOnGet(Name = "pmatch")]
    public object PatternMatch_2_Get() => "get";

    [ActionOnPut(Name = "pmatch")]
    public object PatternMatch_3_Put(string v) => "put: " + v;

    [ActionOnPost(Name = "pmatch", Order = 2)]
    public object PatternMatch_4_Post(string v) => "post: " + v;

    [Action(Name = "pmatch", Order = 1, MatchScript ="methods=POST accept-json=true")]
    public object PatternMatch_5_PostJson(string v) => new {post = v };

    [ActionOnDelete(Name = "pmatch")]
    public object PatternMatch_6_Delete(string v) => "delete: " + v;

    [ActionOnPatch(Name = "pmatch")]
    public object PatternMatch_7_Patch(string v) => "patch: " + v;


    [Action(Name = "filter-get"), HttpGet]
    public object Filter_1_Get() => "get";

    [Action(Name = "filter-put"), HttpPut]
    public object Filter_2_Put(string v) => "put: " + v;

    [Action(Name = "filter-post"), HttpPost]
    public object Filter_3_Post(string v) => "post: " + v;

    [Action(Name = "filter-post-json"), HttpPost, AcceptsJson]
    public object Filter_3_PostJson(string v) => new {post = v };

    [Action(Name = "filter-delete"), HttpDelete]
    public object Filter_4_Delete(string v) => "delete: " + v;

    [Action(Name = "filter-patch"), HttpPatch]
    public object Filter_5_Patch(string v) => "patch: " + v;


  }
}
