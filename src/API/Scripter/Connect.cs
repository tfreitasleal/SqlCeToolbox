using System;
using System.Text;
using EnvDTE;
using Extensibility;
using Microsoft.SqlServer.Management.UI.VSIntegration;
using Microsoft.SqlServer.Management.UI.VSIntegration.ObjectExplorer;
using System.Reflection;
using System.ComponentModel;

namespace SqlCeScripter
{
    /// <summary>The object for implementing an Add-in.</summary>
    /// <seealso class='IDTExtensibility2' />
    public class Connect : IDTExtensibility2
    {
        private HierarchyObject _serverMenu;
        private HierarchyObject _tableMenu;
        private HierarchyObject _viewMenu;
        private HierarchyObject _indexMenu;

        string urnPath = "SqlServerCe";
        string tableUrnPath = "SqlServerCe/UsrTable";
        string viewUrnPath = "SqlServerCe/SysView";
        string indexUrnPath = "SqlServerCe/UsrTable/Index";

        public Connect()
        {
        }

        internal static string ConnectionString { get; set; }
        internal static EnvDTE80.DTE2 CurrentApplication { get; set; }
        internal static EnvDTE.AddIn CurrentAddin { get; set; }
        internal static string CurrentTable { get; set; }
        internal static bool ViewsSelected { get; set; }
        internal static bool EditTracked;
       
        public void OnConnection(object Application, ext_ConnectMode ConnectMode, object AddInInst, ref Array custom)
        {
            Connect.CurrentAddin = (EnvDTE.AddIn)AddInInst;
            Connect.CurrentApplication = (EnvDTE80.DTE2)Connect.CurrentAddin.DTE;

            SetObjectExplorerEventProvider();            
        }

        private void Provider_SelectionChanged(object sender, NodesChangedEventArgs args)
        {
            Connect.ViewsSelected = false;
            INodeInformation node = Connect.ObjectExplorerSelectedNode;
            if (node != null)
            {
                System.Diagnostics.Debug.WriteLine(node.UrnPath);
                System.Diagnostics.Debug.WriteLine(node.Name);
                System.Diagnostics.Debug.WriteLine(node.Context);
                // Mobile Device connections not supported
                if (node.Connection.ServerName.StartsWith("Mobile Device\\", StringComparison.Ordinal))
                {
                    // short circuit
                    node = null;
                }
            }

            if (node != null && _serverMenu == null &&
                urnPath == node.UrnPath)
            {
                _serverMenu = (HierarchyObject)node.GetService(typeof(IMenuHandler));
                
                _serverMenu.AddChild(string.Empty, new ToolStripSeparatorMenuItem());
                DatabaseMenuItem item = new DatabaseMenuItem();
                _serverMenu.AddChild(string.Empty, item);
                ServerMenuItem serveritem = new ServerMenuItem();
                _serverMenu.AddChild(string.Empty, serveritem);
            }

            if (node != null && _tableMenu == null &&
                tableUrnPath == node.UrnPath)
            {
                _tableMenu = (HierarchyObject)node.GetService(typeof(IMenuHandler));

                _tableMenu.AddChild(string.Empty, new ToolStripSeparatorMenuItem());
                EditTableMenuItem itemE = new EditTableMenuItem();
                _tableMenu.AddChild(string.Empty, itemE);

                _tableMenu.AddChild(string.Empty, new ToolStripSeparatorMenuItem());
                ScriptTableMenuItem item = new ScriptTableMenuItem();
                _tableMenu.AddChild(string.Empty, item);

                _tableMenu.AddChild(string.Empty, new ToolStripSeparatorMenuItem());
                ImportTableMenuItem item2 = new ImportTableMenuItem();
                _tableMenu.AddChild(string.Empty, item2);

                _tableMenu.AddChild(string.Empty, new ToolStripSeparatorMenuItem());
                RenameTableMenuItem item3 = new RenameTableMenuItem();
                _tableMenu.AddChild(string.Empty, item3);
            }

            if (node != null && _indexMenu == null && indexUrnPath == node.UrnPath)
            {
                _indexMenu = (HierarchyObject)node.GetService(typeof(IMenuHandler));

                _indexMenu.AddChild(string.Empty, new ToolStripSeparatorMenuItem());
                ScriptIndexMenuItem itemI = new ScriptIndexMenuItem();
                _indexMenu.AddChild(string.Empty, itemI);
            }

            if (node != null && _viewMenu == null &&
                    viewUrnPath == node.UrnPath)
            {
                _viewMenu = (HierarchyObject)node.GetService(typeof(IMenuHandler));

                _viewMenu.AddChild(string.Empty, new ToolStripSeparatorMenuItem());
                EditTableMenuItem itemV = new EditTableMenuItem();
                _viewMenu.AddChild(string.Empty, itemV);
            }

            // Set this each time
            if (node != null && viewUrnPath == node.UrnPath)
            {
                Connect.ViewsSelected = true;
            }
            
        }

        public void OnDisconnection(ext_DisconnectMode RemoveMode, ref Array custom)
        {
        }

        public void OnAddInsUpdate(ref Array custom)
        {
        }

        public void OnStartupComplete(ref Array custom)
        {
        }

        public void OnBeginShutdown(ref Array custom)
        {
        }


        internal static void ShowErrors(System.Data.SqlClient.SqlException e)
        {
            System.Windows.Forms.MessageBox.Show(ErikEJ.SqlCeScripting.Helper.ShowErrors(e));
        }
    
        internal static void ShowErrors(System.Data.SqlServerCe.SqlCeException e)
        {
            System.Windows.Forms.MessageBox.Show(ErikEJ.SqlCeScripting.Helper.ShowErrors(e));
        }

        // Code from http://extendedeventmanager.codeplex.com/ - thanks Jonathan K.
        internal static IObjectExplorerService ObjectExplorer
        {
            get
            {
                return (IObjectExplorerService)ServiceCache.ServiceProvider.GetService(typeof(IObjectExplorerService));
            }
        }

        internal static INodeInformation ObjectExplorerSelectedNode
        {
            get
            {
                int count;
                INodeInformation[] informationArray;
                ObjectExplorer.GetSelectedNodes(out count, out informationArray);
                if (count > 0)
                {
                    return informationArray[0];
                }
                return null;
            }
        }

        public void SetObjectExplorerEventProvider()
        {
            if (Connect.ObjectExplorer == null)
                return;

            System.Type t = Assembly.Load("SqlWorkbench.Interfaces").GetType("Microsoft.SqlServer.Management.UI.VSIntegration.ObjectExplorer.IObjectExplorerEventProvider");
            if (t != null)
                SetNON2008R2ObjectExplorerEventProvider(t);
            else
                Set2008R2ObjectExplorerEventProvider();
        }


        void SetNON2008R2ObjectExplorerEventProvider(System.Type t)
        {
            // the old way of doing things
            //IObjectExplorerEventProvider objectExplorer = (IObjectExplorerEventProvider)Common.ObjectExplorerService.GetService(typeof(IObjectExplorerEventProvider));
            //objectExplorer.SelectionChanged += new NodesChangedEventHandler(objectExplorer_SelectionChanged);

            MethodInfo mi = this.GetType().GetMethod("Provider_SelectionChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            // get the IObjectExplorerEventProvider from the ObjectExplorerService
            object objectExplorer = Connect.ObjectExplorer.GetService(t);
            EventInfo ei = t.GetEvent("SelectionChanged", System.Reflection.BindingFlags.Public | BindingFlags.Instance);
            // use this overload CreateDelegate(Type type, object firstArgument, MethodInfo method);
            // the 2nd param is "this" because the method to handle the event is in it.
            Delegate del = Delegate.CreateDelegate(ei.EventHandlerType, this, mi);
            ei.AddEventHandler(objectExplorer, del);
        }

        void Set2008R2ObjectExplorerEventProvider()
        {
            // the old way of doing things
            //Microsoft.SqlServer.Management.SqlStudio.Explorer.ObjectExplorerService objectExplorer = Common.ObjectExplorerService as Microsoft.SqlServer.Management.SqlStudio.Explorer.ObjectExplorerService;
            //int nodeCount;
            //INodeInformation[] nodes;
            //objectExplorer.GetSelectedNodes(out nodeCount, out nodes);
            //Microsoft.SqlServer.Management.SqlStudio.Explorer.ContextService contextService = (Microsoft.SqlServer.Management.SqlStudio.Explorer.ContextService)objectExplorer.Container.Components[1];
            //// or ContextService contextService = (ContextService)objectExplorer.Site.Container.Components[1];
            //INavigationContextProvider provider = contextService.ObjectExplorerContext;
            //provider.CurrentContextChanged += new NodesChangedEventHandler(ObjectExplorer_SelectionChanged);

            System.Type t = Assembly.Load("Microsoft.SqlServer.Management.SqlStudio.Explorer").GetType("Microsoft.SqlServer.Management.SqlStudio.Explorer.ObjectExplorerService");
            MethodInfo mi = this.GetType().GetMethod("Provider_SelectionChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            int nodeCount;
            INodeInformation[] nodes;
            object objectExplorer = Connect.ObjectExplorer;
            // hack to load the OE in R2
            (objectExplorer as IObjectExplorerService).GetSelectedNodes(out nodeCount, out nodes);

            PropertyInfo piContainer = t.GetProperty("Container", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            object objectExplorerContainer = piContainer.GetValue(objectExplorer, null);
            PropertyInfo piContextService = objectExplorerContainer.GetType().GetProperty("Components", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            //object[] indexArgs = { 1 };
            ComponentCollection objectExplorerComponents = piContextService.GetValue(objectExplorerContainer, null) as ComponentCollection;
            object contextService = null;

            foreach (Component component in objectExplorerComponents)
            {
                if (component.GetType().FullName.Contains("ContextService"))
                {
                    contextService = component;
                    break;
                }
            }
            if (contextService == null)
                throw new NullReferenceException("Can't find ObjectExplorer ContextService.");

            PropertyInfo piObjectExplorerContext = contextService.GetType().GetProperty("ObjectExplorerContext", System.Reflection.BindingFlags.Public | BindingFlags.Instance);
            object objectExplorerContext = piObjectExplorerContext.GetValue(contextService, null);

            EventInfo ei = objectExplorerContext.GetType().GetEvent("CurrentContextChanged", System.Reflection.BindingFlags.Public | BindingFlags.Instance);

            Delegate del = Delegate.CreateDelegate(ei.EventHandlerType, this, mi);
            ei.AddEventHandler(objectExplorerContext, del);
        }
        // Code from http://extendedeventmanager.codeplex.com/ - thanks Jonathan K.

    }
}