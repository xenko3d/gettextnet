using System;
using System.Collections.Generic;
using System.Reflection;
using System.ComponentModel;
using System.Windows.Forms;

namespace GNU.Gettext.WinForms
{
	public class ToolTipControls : List<ToolTip>
	{
	}
	
	public class Localizer
	{
		public delegate void OnIterateControl(Control control);
		
		public GettextResourceManager Catalog { get; set; }
		public ObjectPropertiesStore OriginalTextStore { get; set; }
		
		private ToolTipControls toolTips = new ToolTipControls();
		public ToolTipControls ToolTips
		{
			get { return toolTips; }
		}
		private Control root;
		
		#region Constructors
		public Localizer(Control rootControl, string resourceBaseName)
			: this(rootControl, new GettextResourceManager(resourceBaseName), null)
		{
		}
		
		public Localizer(Control rootControl, string resourceBaseName, ObjectPropertiesStore originalTextStore)
			: this(rootControl, new GettextResourceManager(resourceBaseName), originalTextStore)
		{
		}
		
		public Localizer(Control rootControl, GettextResourceManager catalog)
			: this(rootControl, catalog, null)
		{
		}
		
		public Localizer(Control rootControl, GettextResourceManager catalog, ObjectPropertiesStore originalTextStore)
		{
            this.Catalog = catalog;
			this.OriginalTextStore = originalTextStore;
			this.root = rootControl;

			// Access to form components
			IterateControls(root, 
			               delegate(Control control) {
				InitFromContainer(control.Container);
			});
			for (Control c = root; c != null; c = c.Parent)
			{
				if (c is Form || c is UserControl)
				{
					FieldInfo fi = c.GetType().GetField("components", BindingFlags.NonPublic | BindingFlags.Instance);
					if (fi != null)
					{
						InitFromContainer((IContainer)fi.GetValue(c));
					}
				}
			}
			
			Localize();
		}
		
		private void InitFromContainer(IContainer container)
		{
			if (container == null || container.Components == null)
				return;
			foreach(Component component in container.Components)
			{
				if (component is ToolTip)
				{
					if (!toolTips.Contains(component as ToolTip))
						toolTips.Add(component as ToolTip);
				}
			}
		}
		#endregion
		
		#region Public interface
		public static void Localize(Control control, GettextResourceManager catalog)
		{
			Localizer.Localize(control, catalog, null);
		}
		
		public static void Localize(Control control, GettextResourceManager catalog, ObjectPropertiesStore originalTextStore)
		{
			if (catalog == null)
				return;
			Localizer loc = new Localizer(control, catalog, originalTextStore);
			loc.Localize();
		}

		public static void Revert(Control control, ObjectPropertiesStore originalTextStore)
		{
			Localizer loc = new Localizer(control, new GettextResourceManager(), originalTextStore);
			loc.Revert();
		}
		
		public void Localize()
		{
			IterateControls(root, IterateMode.Localize);
		}
		
		public void Revert()
		{
			IterateControls(root, IterateMode.Revert);
		}
		#endregion
		
		#region Handlers for different types
		enum IterateMode
		{
			Localize,
			Revert
		}
		
		private void IterateControlHandler(LocalizableObjectAdapter adapter, IterateMode mode)
		{
			switch (mode)
			{
			case IterateMode.Localize:
				adapter.Localize(Catalog);
				break;
			case IterateMode.Revert:
				adapter.Revert();
				break;
			}
		}
		
		#endregion
			
		private IContainer FindContainer()
		{
			for (Control c = root; c != null; c = c.Parent)
			{
				if (c.Container != null)
					return c.Container;
			}
			return null;
		}
		
		private void IterateControls(Control control, OnIterateControl onIterateControl)
		{
			if (control is ContainerControl)
			{
				foreach(Control child in (control as ContainerControl).Controls)
				{
					IterateControls(child, onIterateControl);
				}
			}
			
			if (onIterateControl != null)
				onIterateControl(control);
		}
		
		
		private void IterateControls(Control control, IterateMode mode)
		{
			if (control is ContainerControl)
			{
				foreach(Control child in (control as ContainerControl).Controls)
				{
					IterateControls(child, mode);
				}
			}
			
			if (control is ToolStrip)
			{
				foreach(ToolStripItem item in (control as ToolStrip).Items)
				{
					IterateToolStripItems(item, mode);
				}
			}
			
			IterateControlHandler(new LocalizableObjectAdapter(control, OriginalTextStore, toolTips), mode);
		}
		
		private void IterateToolStripItems(ToolStripItem item, IterateMode mode)
		{
			if (item is ToolStripDropDownItem)
			{
				foreach(ToolStripItem subitem in (item as ToolStripDropDownItem).DropDownItems)
				{
					IterateToolStripItems(subitem, mode);
				}
			}
			IterateControlHandler(new LocalizableObjectAdapter(item, OriginalTextStore, toolTips), mode);
		}
	}
}

