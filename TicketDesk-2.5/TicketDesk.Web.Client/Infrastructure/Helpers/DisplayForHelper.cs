﻿// TicketDesk - Attribution notice
// Contributor(s):
//
//      Stephen Redd (stephen@reddnet.net, http://www.reddnet.net)
//
// This file is distributed under the terms of the Microsoft Public 
// License (Ms-PL). See http://opensource.org/licenses/MS-PL
// for the complete terms of use. 
//
// For any distribution that contains code from this file, this notice of 
// attribution must remain intact, and a copy of the license must be 
// provided to the recipient.

using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Web.Mvc;

namespace TicketDesk.Web.Client
{
    public static class DisplayForHelper
    {
       

        public static MvcHtmlString DisplayNameFor<TModel, TValue>(this HtmlHelper<TModel> helper, Expression<Func<TModel, TValue>> expression)
        {
            var md = ModelMetadata.FromLambdaExpression(expression, helper.ViewData);
            return new MvcHtmlString(md.GetDisplayName());
        }


        public static MvcHtmlString DisplayNameFor(this HtmlHelper helper, PropertyInfo property)
        {
            var attr = (DisplayAttribute)property.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault();
            
            return new MvcHtmlString(attr == null || attr.Name == null?  property.Name: attr.Name);
        }
    }
}