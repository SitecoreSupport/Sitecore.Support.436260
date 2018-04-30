using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Pipelines.HttpRequest;

namespace Sitecore.Support.Pipelines.HttpRequest
{
  public class ItemResolver : Sitecore.Pipelines.HttpRequest.ItemResolver
  {
    public override void Process(HttpRequestArgs args)
    {
      base.Process(args);

      Database db = Context.Database;
      if (db == null)
      {
        return;
      }
      Item root = db.GetRootItem();
      if (Context.Item != null && root != null)
      {
        if (Context.Item.Paths.FullPath == root.Paths.FullPath)
        {
          Context.Item = null;
        }
      }
    }
  }
}