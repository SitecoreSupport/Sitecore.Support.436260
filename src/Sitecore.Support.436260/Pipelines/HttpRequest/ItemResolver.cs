namespace Sitecore.Support.Pipelines.HttpRequest
{
    using Sitecore;
    using Sitecore.Collections;
    using Sitecore.Data;
    using Sitecore.Data.Items;
    using Sitecore.Data.Managers;
    using Sitecore.Diagnostics;
    using Sitecore.Globalization;
    using Sitecore.IO;
    using Sitecore.Pipelines.HttpRequest;
    using Sitecore.SecurityModel;
    using Sitecore.Sites;
    using System;
    using System.Linq;

    public class ItemResolver : HttpRequestProcessor
    {
        private Item GetChild(Item item, string itemName)
        {
            ChildList children = item.Children;
            Item item2 = children.FirstOrDefault<Item>(child => child.Name == "*");
            foreach (Item item3 in children)
            {
                if (item3.DisplayName.Equals(itemName, StringComparison.OrdinalIgnoreCase))
                {
                    return item3;
                }
                if (item3.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase))
                {
                    return item3;
                }
            }
            if (item2 != null)
            {
                return item2;
            }
            return null;
        }

        private Item GetSubItem(string path, Item root)
        {
            Item child = root;
            foreach (string str in path.Split(new char[] { '/' }))
            {
                if (str.Length != 0)
                {
                    child = this.GetChild(child, str);
                    if (child == null)
                    {
                        return null;
                    }
                }
            }
            return child;
        }

        public override void Process(HttpRequestArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (((Context.Item == null) && (Context.Database != null)) && (args.Url.ItemPath.Length != 0))
            {
                Profiler.StartOperation("Resolve current item.");
                string path = MainUtil.DecodeName(args.Url.ItemPath);
                Item item = args.GetItem(path);
                if (item == null)
                {
                    path = args.Url.ItemPath;
                    item = args.GetItem(path);
                }
                if (item == null)
                {
                    path = args.LocalPath;
                    item = args.GetItem(path);
                }
                if (item == null)
                {
                    path = MainUtil.DecodeName(args.LocalPath);
                    item = args.GetItem(path);
                }
                SiteContext site = Context.Site;
                string str2 = (site != null) ? site.RootPath : string.Empty;
                if (item == null)
                {
                    path = FileUtil.MakePath(str2, args.LocalPath, '/');
                    item = args.GetItem(path);
                }
                if (item == null)
                {
                    path = MainUtil.DecodeName(FileUtil.MakePath(str2, args.LocalPath, '/'));
                    item = args.GetItem(path);
                }
                if ((item == null) || item.Name.Equals("*"))
                {
                    item = this.ResolveUsingDisplayName(args);
                }
                if (((item == null) && args.UseSiteStartPath) && (site != null))
                {
                    item = args.GetItem(site.StartPath);
                }
                if (item != null)
                {
                    Tracer.Info("Current item is \"" + path + "\".");
                }
                Context.Item = item;
                Profiler.EndOperation();
            }
        }

        private Item ResolveFullPath(HttpRequestArgs args)
        {
            string itemPath = args.Url.ItemPath;
            if (string.IsNullOrEmpty(itemPath) || (itemPath[0] != '/'))
            {
                return null;
            }
            int index = itemPath.IndexOf('/', 1);
            if (index < 0)
            {
                return null;
            }
            Item root = ItemManager.GetItem(itemPath.Substring(0, index), Language.Current, Sitecore.Data.Version.Latest, Context.Database, SecurityCheck.Disable);
            if (root == null)
            {
                return null;
            }
            string path = MainUtil.DecodeName(itemPath.Substring(index));
            Item subItem = this.GetSubItem(path, root);
            if ((subItem == null) && (args.LocalPath.Length > index))
            {
                path = MainUtil.DecodeName(args.LocalPath.Substring(index));
                subItem = this.GetSubItem(path, root);
            }
            return subItem;
        }

        private Item ResolveLocalPath(HttpRequestArgs args)
        {
            SiteContext site = Context.Site;
            if (site == null)
            {
                return null;
            }
            Item root = ItemManager.GetItem(site.RootPath, Language.Current, Sitecore.Data.Version.Latest, Context.Database, SecurityCheck.Disable);
            if (root == null)
            {
                return null;
            }
            string path = MainUtil.DecodeName(args.LocalPath);
            return this.GetSubItem(path, root);
        }

        private Item ResolveUsingDisplayName(HttpRequestArgs args)
        {
            Item item;
            Assert.ArgumentNotNull(args, "args");
            using (new SecurityDisabler())
            {
                item = this.ResolveLocalPath(args);
                if (item == null)
                {
                    item = this.ResolveFullPath(args);
                }
                if (item == null)
                {
                    return null;
                }
            }
            return args.ApplySecurity(item);
        }
    }
}