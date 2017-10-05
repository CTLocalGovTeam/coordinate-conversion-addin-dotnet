// Copyright 2016 Esri 
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Threading;
using System.Windows.Forms;
using ESRI.ArcGIS.ArcMapUI;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Geometry;

namespace ArcMapAddinCoordinateConversion.Helpers
{
    public class ArcMapHelpers
    {
        public static ISpatialReference GetGCS_WGS_1984_SR()
        {
            Type t = Type.GetTypeFromProgID("esriGeometry.SpatialReferenceEnvironment");
            Object obj = Activator.CreateInstance(t);
            ISpatialReferenceFactory srFact = (ISpatialReferenceFactory) obj;

            // Use the enumeration to create an instance of the predefined object.

            IGeographicCoordinateSystem geographicCS =
                srFact.CreateGeographicCoordinateSystem((int)
                esriSRGeoCSType.esriSRGeoCS_WGS1984);

            return geographicCS;
        }
        public static ISpatialReference GetSR(int type)
        {
            Type t = Type.GetTypeFromProgID("esriGeometry.SpatialReferenceEnvironment");
            Object obj = Activator.CreateInstance(t);
            ISpatialReferenceFactory srFact = (ISpatialReferenceFactory) obj;

            // Use the enumeration to create an instance of the predefined object.
            try
            {
                IGeographicCoordinateSystem geographicCS = srFact.CreateGeographicCoordinateSystem(type);

                return geographicCS;
            }
            catch
            {
                // do nothing
            }


            try
            {
                IProjectedCoordinateSystem projectCS = srFact.CreateProjectedCoordinateSystem(type);

                return projectCS;
            }
            catch
            {
                // do nothing
            }

            return null;
        }
        /// <summary>
        /// Adds a graphic element to the map graphics container
        /// Returns GUID
        /// </summary>
        /// <param name="geom"></param>
        /// <param name="color"></param>
        /// <param name="IsTempGraphic"></param>
        /// <param name="markerStyle"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static string AddGraphicToMap(IGeometry geom, IColor color, bool IsTempGraphic = false, esriSimpleMarkerStyle markerStyle = esriSimpleMarkerStyle.esriSMSCircle, int size = 5)
        {
            if (geom == null || ArcMap.Document == null || ArcMap.Document.FocusMap == null)
                return string.Empty;

            IElement element = null;
            double width = 2.0;

            geom.Project(ArcMap.Document.FocusMap.SpatialReference);

            if (geom.GeometryType == esriGeometryType.esriGeometryPoint)
            {
                // Marker symbols
                var simpleMarkerSymbol = (ISimpleMarkerSymbol) new SimpleMarkerSymbol();
                simpleMarkerSymbol.Color = color;
                simpleMarkerSymbol.Outline = false;
                simpleMarkerSymbol.OutlineColor = color;
                simpleMarkerSymbol.Size = size;
                simpleMarkerSymbol.Style = markerStyle;

                var markerElement = (IMarkerElement) new MarkerElement();
                markerElement.Symbol = simpleMarkerSymbol;
                element = markerElement as IElement;
            }
            else if (geom.GeometryType == esriGeometryType.esriGeometryPolyline)
            {
                // create graphic then add to map
                var le = new LineElementClass() as ILineElement;
                element = (IElement) le;

                var lineSymbol = new SimpleLineSymbolClass
                {
                    Color = color,
                    Width = width
                };

                le.Symbol = lineSymbol;
            }
            else if (geom.GeometryType == esriGeometryType.esriGeometryPolygon)
            {
                // create graphic then add to map
                IPolygonElement pe = new PolygonElementClass();
                element = (IElement) pe;
                IFillShapeElement fe = pe as IFillShapeElement;

                var fillSymbol = new SimpleFillSymbolClass();
                RgbColor selectedColor = new RgbColorClass();
                selectedColor.Red = 0;
                selectedColor.Green = 0;
                selectedColor.Blue = 0;

                selectedColor.Transparency = 0;
                fillSymbol.Color = selectedColor;

                fe.Symbol = fillSymbol;
            }

            if (element == null)
                return string.Empty;

            element.Geometry = geom;

            var mxdoc = (IMxDocument) ArcMap.Application.Document;
            var av = mxdoc.FocusMap as IActiveView;
            var gc = (IGraphicsContainer) av;

            // store guid
            var eprop = (IElementProperties) element;
            eprop.Name = Guid.NewGuid().ToString();

            gc?.AddElement(element, 0);

            av?.PartialRefresh(esriViewDrawPhase.esriViewGraphics, null, null);

            return eprop.Name;
        }

        ///<summary>Flash geometry on the display. The geometry type could be polygon, polyline, point, or multipoint.</summary>
        ///
        ///<param name="geometry"> An IGeometry interface</param>
        ///<param name="color">An IRgbColor interface</param>
        ///<param name="display">An IDisplay interface</param>
        ///<param name="delay">A System.Int32 that is the time im milliseconds to wait.</param>
        /// <param name="envelope">IEnvelope</param>
        /// 
        ///<remarks></remarks>
        public static void FlashGeometry(IGeometry geometry, IRgbColor color, IDisplay display, Int32 delay, IEnvelope envelope)
        {
            if (geometry == null || color == null || display == null)
            {
                return;
            }

            display.StartDrawing(display.hDC, (Int16)esriScreenCache.esriNoScreenCache); // Explicit Cast

            switch (geometry.GeometryType)
            {
                case esriGeometryType.esriGeometryPolygon:
                    {
                        //Set the flash geometry's symbol.
                        ISimpleFillSymbol simpleFillSymbol = new SimpleFillSymbolClass();
                        simpleFillSymbol.Color = color;
                        var symbol = (ISymbol) simpleFillSymbol; // Dynamic Cast
                        symbol.ROP2 = esriRasterOpCode.esriROPNotXOrPen;

                        //Flash the input polygon geometry.
                        display.SetSymbol(symbol);
                        display.DrawPolygon(geometry);
                        Thread.Sleep(delay);
                        display.DrawPolygon(geometry);
                        break;
                    }

                case esriGeometryType.esriGeometryPolyline:
                    {
                        //Set the flash geometry's symbol.
                        ISimpleLineSymbol simpleLineSymbol = new SimpleLineSymbolClass();
                        simpleLineSymbol.Width = 4;
                        simpleLineSymbol.Color = color;
                        var symbol = (ISymbol) simpleLineSymbol; // Dynamic Cast
                        symbol.ROP2 = esriRasterOpCode.esriROPNotXOrPen;

                        //Flash the input polyline geometry.
                        display.SetSymbol(symbol);
                        display.DrawPolyline(geometry);
                        Thread.Sleep(delay);
                        display.DrawPolyline(geometry);
                        break;
                    }

                case esriGeometryType.esriGeometryPoint:
                    {
                        //Set the flash geometry's symbol.
                        ISimpleMarkerSymbol simpleMarkerSymbol = new SimpleMarkerSymbolClass();
                        simpleMarkerSymbol.Style = esriSimpleMarkerStyle.esriSMSCircle;
                        simpleMarkerSymbol.Size = 12;
                        simpleMarkerSymbol.Color = color;
                        ISymbol markerSymbol = (ISymbol) simpleMarkerSymbol; // Dynamic Cast
                        markerSymbol.ROP2 = esriRasterOpCode.esriROPNotXOrPen;

                        ISimpleLineSymbol simpleLineSymbol = new SimpleLineSymbolClass();
                        simpleLineSymbol.Width = 1;
                        simpleLineSymbol.Color = color;
                        ISymbol lineSymbol = (ISymbol) simpleLineSymbol; // Dynamic Cast
                        lineSymbol.ROP2 = esriRasterOpCode.esriROPNotXOrPen;

                        //Flash the input polygon geometry.
                        display.SetSymbol(markerSymbol);
                        display.SetSymbol(lineSymbol);

                        DrawCrossHair(geometry, display, envelope, markerSymbol, lineSymbol);

                        //Flash the input point geometry.
                        display.SetSymbol(markerSymbol);
                        display.DrawPoint(geometry);
                        Thread.Sleep(delay);
                        display.DrawPoint(geometry);
                        break;
                    }

                case esriGeometryType.esriGeometryMultipoint:
                    {
                        //Set the flash geometry's symbol.
                        ISimpleMarkerSymbol simpleMarkerSymbol = new SimpleMarkerSymbolClass();
                        simpleMarkerSymbol.Style = esriSimpleMarkerStyle.esriSMSCircle;
                        simpleMarkerSymbol.Size = 12;
                        simpleMarkerSymbol.Color = color;
                        ISymbol symbol = (ISymbol) simpleMarkerSymbol; // Dynamic Cast
                        symbol.ROP2 = esriRasterOpCode.esriROPNotXOrPen;

                        //Flash the input multipoint geometry.
                        display.SetSymbol(symbol);
                        display.DrawMultipoint(geometry);
                        Thread.Sleep(delay);
                        display.DrawMultipoint(geometry);
                        break;
                    }
            }

            display.FinishDrawing();
        }

        private static void DrawCrossHair(IGeometry geometry, IDisplay display, IEnvelope extent, ISymbol markerSymbol, ISymbol lineSymbol)
        {
            try
            {
                var point = geometry as IPoint;
                if (point == null) return;
                var numSegments = 10;

                var latitudeMid = point.Y;//envelope.YMin + ((envelope.YMax - envelope.YMin) / 2);
                var longitudeMid = point.X;
                var leftLongSegment = (point.X - extent.XMin) / numSegments;
                var rightLongSegment = (extent.XMax - point.X) / numSegments;
                var topLatSegment = (extent.YMax - point.Y) / numSegments;
                var bottomLatSegment = (point.Y - extent.YMin) / numSegments;
                var fromLeftLong = extent.XMin;
                var fromRightLong = extent.XMax;
                var fromTopLat = extent.YMax;
                var fromBottomLat = extent.YMin;
                var av = ((IMxDocument) ArcMap.Application.Document).ActiveView;

                var leftPolyline = new PolylineClass();
                var rightPolyline = new PolylineClass();
                var topPolyline = new PolylineClass();
                var bottomPolyline = new PolylineClass();

                leftPolyline.SpatialReference = geometry.SpatialReference;
                rightPolyline.SpatialReference = geometry.SpatialReference;
                topPolyline.SpatialReference = geometry.SpatialReference;
                bottomPolyline.SpatialReference = geometry.SpatialReference;

                var leftPC = (IPointCollection) leftPolyline;
                var rightPC = (IPointCollection) rightPolyline;
                var topPC = (IPointCollection) topPolyline;
                var bottomPC = (IPointCollection) bottomPolyline;

                leftPC.AddPoint(new PointClass { X = fromLeftLong, Y = latitudeMid });
                rightPC.AddPoint(new PointClass { X = fromRightLong, Y = latitudeMid });
                topPC.AddPoint(new PointClass { X = longitudeMid, Y = fromTopLat });
                bottomPC.AddPoint(new PointClass { X = longitudeMid, Y = fromBottomLat });

                for (int x = 1; x <= numSegments; x++)
                {
                    //Flash the input polygon geometry.
                    display.SetSymbol(markerSymbol);
                    display.SetSymbol(lineSymbol);

                    leftPC.AddPoint(new PointClass { X = fromLeftLong + leftLongSegment * x, Y = latitudeMid });
                    rightPC.AddPoint(new PointClass { X = fromRightLong - rightLongSegment * x, Y = latitudeMid });
                    topPC.AddPoint(new PointClass { X = longitudeMid, Y = fromTopLat - topLatSegment * x });
                    bottomPC.AddPoint(new PointClass { X = longitudeMid, Y = fromBottomLat + bottomLatSegment * x });

                    // draw
                    display.DrawPolyline(leftPolyline);
                    display.DrawPolyline(rightPolyline);
                    display.DrawPolyline(topPolyline);
                    display.DrawPolyline(bottomPolyline);

                    Thread.Sleep(15);
                    display.FinishDrawing();
                    av.PartialRefresh(esriViewDrawPhase.esriViewForeground, null, null);
                    //av.Refresh();
                    Application.DoEvents();
                    display.StartDrawing(display.hDC, (Int16)esriScreenCache.esriNoScreenCache); // Explicit Cast
                }
            }
            catch 
            {
                // ignored
            }
        }

    }
}
