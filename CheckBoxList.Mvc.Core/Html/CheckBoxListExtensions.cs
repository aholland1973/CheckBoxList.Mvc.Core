using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.Encodings.Web;
using System.Web;


namespace CheckBoxList.Mvc.Core.Html
{
    public static class CheckBoxListExtensions
    {
        // added method to replace ExpressionText as it's not accessible in .NET 5, GetExpressionText is the recommended replacement.
        public static string GetExpressionText<TModel, TResult>(
        this IHtmlHelper<TModel> htmlHelper,
        Expression<Func<TModel, TResult>> expression)
        {
            var expresionProvider = htmlHelper.ViewContext.HttpContext.RequestServices
                .GetService(typeof(ModelExpressionProvider)) as ModelExpressionProvider;

            return expresionProvider.GetExpressionText(expression);
        }

        public static HtmlString CheckBoxList(this IHtmlHelper htmlHelper, string name, IEnumerable<CheckBoxListItem> checkboxList)
        {
            return CheckBoxListHelper(htmlHelper, name, checkboxList, null);
        }

        public static HtmlString CheckBoxList(this IHtmlHelper htmlHelper, string name, IEnumerable<CheckBoxListItem> checkboxList, object htmlAttributes)
        {
            return CheckBoxListHelper(htmlHelper, name, checkboxList, HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
        }

        public static HtmlString CheckBoxList(this IHtmlHelper htmlHelper, string name, IEnumerable<CheckBoxListItem> checkboxList, IDictionary<string, object> htmlAttributes)
        {
            return CheckBoxListHelper(htmlHelper, name, checkboxList, htmlAttributes);
        }

        public static HtmlString CheckBoxListFor<TModel, TProperty>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression)
            where TProperty : IEnumerable<CheckBoxListItem>
        {
            return CheckBoxListFor(htmlHelper, expression, null);
        }

        public static HtmlString CheckBoxListFor<TModel, TProperty>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, object htmlAttributes)
            where TProperty : IEnumerable<CheckBoxListItem>
        {
            return CheckBoxListFor(htmlHelper, expression, HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
        }

        public static HtmlString CheckBoxListFor<TModel, TProperty>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, IDictionary<string, object> htmlAttributes)
            where TProperty : IEnumerable<CheckBoxListItem>
        {
            if (expression == null)
                throw new ArgumentNullException("expression");

            var name = htmlHelper.GetExpressionText(expression);

            var func = expression.Compile();
            var checkboxList = func(htmlHelper.ViewData.Model) as IEnumerable<CheckBoxListItem>;

            return CheckBoxListHelper(htmlHelper, name, checkboxList, htmlAttributes);
        }

        public static HtmlString EnumCheckBoxList<T>(this IHtmlHelper htmlHelper, string name, IEnumerable<T> list) where T : struct
        {
            return EnumCheckBoxList(htmlHelper, name, list, null);
        }

        public static HtmlString EnumCheckBoxList<T>(this IHtmlHelper htmlHelper, string name, IEnumerable<T> list, object htmlAttributes) where T : struct
        {
            return EnumCheckBoxList(htmlHelper, name, list, HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
        }

        public static HtmlString EnumCheckBoxList<T>(this IHtmlHelper htmlHelper, string name, IEnumerable<T> list, IDictionary<string, object> htmlAttributes) where T : struct
        {
            if (!typeof(T).IsEnum)
                throw new ArgumentException("T must be an enum type");

            //text, value and if selected
            var tupleList = new List<Tuple<string, int, bool>>();
            foreach (var value in Enum.GetValues(typeof(T)).Cast<T>())
            {
                var selected = list.Contains(value);
                tupleList.Add(new Tuple<string, int, bool>(GetDisplayName(value), Convert.ToInt32(value), selected));
            }

            return EnumCheckBoxListHelper(htmlHelper, name, tupleList, htmlAttributes);
        }

        public static HtmlString EnumCheckBoxListFor<TModel, TProperty>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression)
            where TProperty : IEnumerable
        {
            return EnumCheckBoxListFor(htmlHelper, expression, null);
        }

        public static HtmlString EnumCheckBoxListFor<TModel, TProperty>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, object htmlAttributes)
            where TProperty : IEnumerable
        {
            return EnumCheckBoxListFor(htmlHelper, expression, HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
        }

        public static HtmlString EnumCheckBoxListFor<TModel, TProperty>(this IHtmlHelper<TModel> htmlHelper, Expression<Func<TModel, TProperty>> expression, IDictionary<string, object> htmlAttributes)
            where TProperty : IEnumerable
        {
            if (expression == null)
                throw new ArgumentNullException("expression");

            var name = htmlHelper.GetExpressionText(expression);
            var func = expression.Compile();
            var enumList = func(htmlHelper.ViewData.Model);

            var enumType = enumList.GetType().IsGenericType
                ? enumList.GetType().GetGenericArguments()[0]
                : enumList.GetType().GetElementType();

            if (!enumType.IsEnum)
                throw new ArgumentException("Must be a list of enum type");

            var tupleList = new List<Tuple<string, int, bool>>();
            foreach (var value in Enum.GetValues(enumType))
            {
                var selected = enumList.Cast<object>().Any(s => s.ToString() == value.ToString());
                tupleList.Add(new Tuple<string, int, bool>(GetDisplayName(value), (int)value, selected));
            }

            return EnumCheckBoxListHelper(htmlHelper, name, tupleList, htmlAttributes);
        }

        private static HtmlString CheckBoxListHelper(IHtmlHelper htmlHelper, string name, IEnumerable<CheckBoxListItem> checkboxList, IDictionary<string, object> htmlAttributes)
        {
            var fullName = htmlHelper.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(name);
            if (string.IsNullOrEmpty(fullName))
            {
                throw new ArgumentException("name");
            }

            var listItemBuilder = BuildCheckBoxListItems(htmlHelper, name, checkboxList.ToList());

            var tagBuilder = new TagBuilder("div");
            tagBuilder.TagRenderMode = TagRenderMode.Normal;
            tagBuilder.InnerHtml.AppendHtml(listItemBuilder.ToString());
            tagBuilder.MergeAttributes(htmlAttributes);
            tagBuilder.GenerateId(fullName, string.Empty);

            using (var writer = new StringWriter())
            {
                tagBuilder.WriteTo(writer, HtmlEncoder.Default);
                return new HtmlString(writer.ToString());
            }            
        }
        
        private static StringBuilder BuildCheckBoxListItems(this IHtmlHelper htmlHelper, string name, IList<CheckBoxListItem> list)
        {
            var listItemBuilder = new StringBuilder();

            for (var i = 0; i < list.Count(); i++)
            {
                var item = list[i];

                var checkbox = htmlHelper.CheckBox(GetChildControlName(name, i, "IsChecked"), item.IsChecked);
                var text = htmlHelper.Hidden(GetChildControlName(name, i, "Text"), item.Text);
                var value = htmlHelper.Hidden(GetChildControlName(name, i, "Value"), item.Value);

                var sb = new StringBuilder();
                using (var writer = new StringWriter())
                {
                    sb.AppendLine("<div>");
                    checkbox.WriteTo(writer, HtmlEncoder.Default);
                    text.WriteTo(writer, HtmlEncoder.Default);
                    value.WriteTo(writer, HtmlEncoder.Default);
                    sb.Append(writer.ToString());
                    sb.AppendLine(HttpUtility.HtmlEncode(item.Text));
                }
                sb.AppendLine("</div>");

                listItemBuilder.AppendLine(sb.ToString());
            }

            return listItemBuilder;
        }

        private static string GetChildControlName(string parentName, int index, string childName)
        {
            return string.Format("{0}[{1}].{2}", parentName, index, childName);
        }
        
        private static HtmlString EnumCheckBoxListHelper(IHtmlHelper htmlHelper, string name, IEnumerable<Tuple<string, int, bool>> list, IDictionary<string, object> htmlAttributes)
        {
            var fullName = htmlHelper.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(name);
            if (string.IsNullOrEmpty(fullName))
            {
                throw new ArgumentException("name");
            }

            var listItemBuilder = BuildEnumCheckBoxListItems(fullName, list);

            var tagBuilder = new TagBuilder("div");
            tagBuilder.TagRenderMode = TagRenderMode.Normal;
            tagBuilder.InnerHtml.AppendHtml(listItemBuilder.ToString());
            tagBuilder.MergeAttributes(htmlAttributes);
            tagBuilder.GenerateId(fullName, String.Empty);
            
            using (var writer = new StringWriter())
            {
                tagBuilder.WriteTo(writer, HtmlEncoder.Default);
                return new HtmlString(writer.ToString());
            }
        }

        private static HtmlString BuildEnumCheckBoxListItems(string name, IEnumerable<Tuple<string, int, bool>> list)
        {
            var listItemBuilder = new StringBuilder();
            foreach (var t in list)
            {
                listItemBuilder.AppendLine("<div>");
                var checkBox = string.Format(@"<input name=""{0}"" type=""checkbox"" value=""{1}"" {2} />", name, t.Item2, t.Item3 ? @"checked=""checked""" : string.Empty);
                listItemBuilder.AppendLine(checkBox);
                listItemBuilder.AppendLine(t.Item1);
                listItemBuilder.AppendLine("</div>");
            }

            return new HtmlString(listItemBuilder.ToString());
        }

        private static string GetDisplayName(object value)
        {
            var type = value.GetType();
            var member = type.GetMember(value.ToString());

            var displayAttributes = member[0].GetCustomAttributes(typeof(DisplayAttribute), false) as DisplayAttribute[];
            if (displayAttributes != null && displayAttributes.Any())
                return displayAttributes.First().Name;

            var descriptionAttributes = member[0].GetCustomAttributes(typeof(DescriptionAttribute), false) as DescriptionAttribute[];
            if (descriptionAttributes != null && descriptionAttributes.Any())
                return descriptionAttributes.First().Description;

            return value.ToString();
        }
    }
}
