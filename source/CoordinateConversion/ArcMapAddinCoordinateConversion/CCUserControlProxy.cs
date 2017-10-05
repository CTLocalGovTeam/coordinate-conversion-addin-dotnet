﻿using System;
using System.Windows.Controls;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Framework;
using CoordinateConversionLibrary.Helpers;

namespace ArcMapAddinCoordinateConversion
{
    public class CCUserControlProxy : UserControl
    {
        public CCUserControlProxy()
            : base()
        {
            //InitializeComponent();
        }

        public IApplication ArcMapApplication
        {
            set
            {
                ArcMap.Application = value;
                SyncEvents();
            }
        }

        IActiveViewEvents_Event avEvents = null;

        private void SyncEvents()
        {
            ArcMap.Events.NewDocument += ArcMap_NewOpenDocument;
            ArcMap.Events.OpenDocument += ArcMap_NewOpenDocument;
        }

        private void ArcMap_NewOpenDocument()
        {
            if (avEvents != null)
            {
                avEvents.SelectionChanged -= OnSelectionChanged;
                avEvents = null;
            }
            avEvents = (IActiveViewEvents_Event) ArcMap.Document.ActiveView;
            avEvents.SelectionChanged += OnSelectionChanged;
        }

        private void OnSelectionChanged()
        {
            if (ArcMap.Document.FocusMap.SelectionCount > 0)
            {
                for (int i = 0; i < ArcMap.Document.FocusMap.LayerCount; i++)
                {
                    if (ArcMap.Document.FocusMap.Layer[i] is IFeatureLayer)
                    {
                        var fl = ArcMap.Document.FocusMap.Layer[i] as IFeatureLayer;

                        var fselection = fl as IFeatureSelection;
                        if (fselection == null)
                            continue;

                        if (fselection.SelectionSet.Count == 1)
                        {
                            ICursor cursor;
                            fselection.SelectionSet.Search(null, false, out cursor);

                            var fc = (IFeatureCursor) cursor;
                            var f = fc.NextFeature();

                            if (f?.Shape is IPoint)
                            {
                                var point = (IPoint) f.Shape;
                                if (point != null)
                                {
                                    var tempX = point.X;
                                    var tempY = point.Y;

                                    Mediator.NotifyColleagues(CoordinateConversionLibrary.Constants.NewMapPointSelection, point);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Implementation class of the dockable window add-in. It is responsible for 
        /// creating and disposing the user interface class of the dockable window.
        /// </summary>
        public class AddinImpl : ESRI.ArcGIS.Desktop.AddIns.DockableWindow
        {
            private System.Windows.Forms.Integration.ElementHost m_windowUI;

            public AddinImpl()
            {
            }

            protected override IntPtr OnCreateChild()
            {
                m_windowUI = new System.Windows.Forms.Integration.ElementHost();
                m_windowUI.Child = new DockableWindowCoordinateConversion();
                return m_windowUI.Handle;
            }

            protected override void Dispose(bool disposing)
            {
                if (m_windowUI != null)
                    m_windowUI.Dispose();

                base.Dispose(disposing);
            }

        }
    }
}
