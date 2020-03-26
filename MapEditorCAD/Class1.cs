using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace MapEditorCAD
{

    public class Map
    {
        public string MapName = "";
        public double x = -1;
        public double y = -1;
        public double width = -1;
        public double height = -1;
        public ObjectId ID;
    }
    public class WorldMap
    {
        public double width = -1;
        public double height = -1;
        public Dictionary<string, List<Map>> maps;
    }
    public class MapEditor
    {
        static bool debug = true;
        static string debugPath = "C:\\nzx";
        private string mapDir;
        private string CurrentWorldMap;
        private string CurrentLayer;
        private Point2d P;
        private ObjectId WorldEdgeRect;
        Editor ed;
        //key1 大地图名称
        //Key2 layer

        private Dictionary<string, WorldMap> AllMap;
        public MapEditor()
        {
            AllMap = new Dictionary<string, WorldMap>();
            mapDir = debugPath+ "\\ZhuxianClient\\gamedata\\mapdata";
            ed = Application.DocumentManager.MdiActiveDocument.Editor;
        }
        private void ReadMapData()
        {
            AllMap.Clear();
            string path = mapDir + "\\WorldMapData.json";
            if (File.Exists(path))
            {
                string content = File.ReadAllText(path);
                JObject jstr = JObject.Parse(content);
                foreach (var pair in jstr)
                {
                    WorldMap WMap = new WorldMap { maps = new Dictionary<string, List<Map>>() };
                    WMap.width = Convert.ToDouble((pair.Value as JObject)["width"].ToString());
                    WMap.height = Convert.ToDouble((pair.Value as JObject)["height"].ToString());
                    foreach (KeyValuePair<string,JToken> pair2 in pair.Value["maps"] as JObject)
                    {
                        List<Map> mapList = new List<Map>();
                        JArray Jar = pair2.Value as JArray;
                        foreach (JObject m in Jar)
                        {
                            Map map = new Map();
                            map.x = Convert.ToDouble(m["x"].ToString());
                            map.y = Convert.ToDouble(m["y"].ToString());
                            map.width = Convert.ToDouble(m["width"].ToString());
                            map.height = Convert.ToDouble(m["height"].ToString());
                            map.MapName = m["mapName"].ToString();
                            mapList.Add(map);
                        }
                        WMap.maps.Add(pair2.Key, mapList);
                    }
                    AllMap.Add(pair.Key, WMap);
                }
            }
            else
            {
                if (debug)
                {
                    WorldMap WMap = new WorldMap() { maps = new Dictionary<string, List<Map>>() };
                    WMap.width = 1000;
                    WMap.height = 1000;
                    //Dictionary<string, List<Map>> Dic = new Dictionary<string, List<Map>>();
                    string[] layers = { "B2", "B1", "Ground", "F1", "F2" };
                    foreach (string layer in layers)
                    {
                        List<Map> maps = new List<Map>();
                        foreach (string MapPath in Directory.EnumerateDirectories(this.mapDir))
                        {
                            if (File.Exists(MapPath + "\\" + layer + ".png"))
                            {
                                Map m = new Map();
                                m.MapName = Path.GetFileName(MapPath);
                                m.x = -1;
                                m.y = -1;
                                m.width = -1;
                                m.height = -1;
                                maps.Add(m);
                            }
                        }
                        WMap.maps.Add(layer, maps);
                    }
                    AllMap.Add("大世界", WMap);
                }
                else
                {

                }
            }
        }
        private void WriteMapData()
        {
            string path = mapDir + "\\WorldMapData.json";
            if (File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage("文件被占用，请重试");
                    return;
                }
            }
            StreamWriter sw = File.CreateText(path);
            JObject alldata = new JObject();
            foreach (var pair in AllMap)
            {
                JObject WorldmapData = new JObject();
                WorldmapData.Add("width", pair.Value.width);
                WorldmapData.Add("height", pair.Value.height);
                JObject layerData = new JObject();
                foreach (var pair2 in pair.Value.maps)
                {
                    JArray mapData = new JArray();
                    foreach (var iter in pair2.Value)
                    {
                        JObject aMap = new JObject();
                        aMap.Add("mapName", iter.MapName);
                        //string.Format("{0:G2}", iter.height);
                        aMap.Add("x", iter.x);
                        aMap.Add("y", iter.y);
                        aMap.Add("width", iter.width);
                        aMap.Add("height", iter.height);
                        mapData.Add(aMap);
                    }
                    layerData.Add(pair2.Key, mapData);
                }
                WorldmapData.Add("maps", layerData);
                alldata.Add(pair.Key, WorldmapData);
            }
            sw.Write(alldata.ToString());
            sw.Flush();
        }

        
        ObjectId imageID;
        [CommandMethod("InputMap")]
        public void InputMap()
        {
            string WolrdMapName = "大世界";
            string layer = "Ground";
            CurrentWorldMap = WolrdMapName;
            CurrentLayer = layer;
            //line1 = new Line();
            //Point3d point1 = new Point3d(0, 0, 0);
            //Point3d point2 = new Point3d(100, 0, 0);
            //line1.StartPoint = point1;
            //line1.EndPoint = point2;

            //using (Transaction trans = db.TransactionManager.StartTransaction())
            //{

            //    btr.AppendEntity(line1);
            //    trans.AddNewlyCreatedDBObject(line1,true);
            //    trans.Commit();
            //}

            // Get the current database and start a transaction
            Database acCurDb;
            acCurDb = Application.DocumentManager.MdiActiveDocument.Database;
           
            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                // Define the name and image to use

                
                RasterImageDef acRasterDef;
                bool bRasterDefCreated = false;
                ObjectId acImgDefId;

                // Get the image dictionary
                ObjectId acImgDctID = RasterImageDef.GetImageDictionary(acCurDb);

                // Check to see if the dictionary does not exist, it not then create it
                if (acImgDctID.IsNull)
                {
                    acImgDctID = RasterImageDef.CreateImageDictionary(acCurDb);
                }

                // Open the image dictionary
                DBDictionary acImgDict = acTrans.GetObject(acImgDctID, OpenMode.ForRead) as DBDictionary;

                
                ReadMapData();

                //创建世界地图的边缘矩形
                Document doc = Application.DocumentManager.MdiActiveDocument;
                Database db = doc.Database;
                BlockTable bt = acTrans.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = acTrans.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                double worldWidth = AllMap[CurrentWorldMap].width;
                double worldHeight = AllMap[CurrentWorldMap].height;

                Point2d p1 = new Point2d(0, 0);
                Point2d p2 = new Point2d(worldWidth, 0);
                Point2d p3 = new Point2d(worldWidth, worldHeight);
                Point2d p4 = new Point2d(0, worldHeight);
                Polyline Rect = new Polyline();
                Rect.AddVertexAt(0, p1, 0, 0, 0);
                Rect.AddVertexAt(1, p2, 0, 0, 0);
                Rect.AddVertexAt(2, p3, 0, 0, 0);
                Rect.AddVertexAt(3, p4, 0, 0, 0);
                
                Rect.Closed = true;
                WorldEdgeRect = btr.AppendEntity(Rect);
                acTrans.AddNewlyCreatedDBObject(Rect, true);
                //Line line1 = new Line() { StartPoint = new Point3d(0, 0, 0), EndPoint = new Point3d(worldWidth, 0, 0) };
                //Line line2 = new Line() { StartPoint = new Point3d(0, worldWidth, 0), EndPoint = new Point3d(worldWidth, worldHeight, 0) };
                //Line line3 = new Line() { StartPoint = new Point3d(worldWidth, worldHeight, 0), EndPoint = new Point3d(0, worldHeight, 0) };
                //Line line4 = new Line() { StartPoint = new Point3d(0, worldHeight, 0), EndPoint = new Point3d(0, 0, 0) };
                //btr.AppendEntity(line1);
                //btr.AppendEntity(line2);
                //btr.AppendEntity(line3);
                //btr.AppendEntity(line4);
                //acTrans.AddNewlyCreatedDBObject(line1, true);
                //acTrans.AddNewlyCreatedDBObject(line2, true);
                //acTrans.AddNewlyCreatedDBObject(line3, true);
                //acTrans.AddNewlyCreatedDBObject(line4, true);
                //Entity ent1 = (Entity)line1;
                //Entity ent2 = (Entity)line2;
                //Entity ent3 = (Entity)line3;
                //Entity ent4 = (Entity)line4;
                //Group group = new Group();
                //group.Append(ent1.ObjectId);
                //group.Append(ent2.ObjectId);
                //group.Append(ent3.ObjectId);
                //group.Append(ent4.ObjectId);
                //DBDictionary groupdic = acTrans.GetObject(db.GroupDictionaryId, OpenMode.ForWrite) as DBDictionary;
                //group.Selectable = true;
                //group.Name = "mapEdge";
                //groupdic.SetAt(group.Name, group);
                //acTrans.AddNewlyCreatedDBObject(group, true);
                //~创建世界地图的边缘矩形
                foreach (var iter in AllMap[WolrdMapName].maps[layer])
                {
                    string strImgName = iter.MapName;
                    string strFileName = mapDir + "\\" + iter.MapName + "\\" + layer + ".png";
                    if (acImgDict.Contains(strImgName))
                    {
                        acImgDefId = acImgDict.GetAt(strImgName);

                        acRasterDef = acTrans.GetObject(acImgDefId, OpenMode.ForWrite) as RasterImageDef;
                    }
                    else
                    {
                        // Create a raster image definition
                        RasterImageDef acRasterDefNew = new RasterImageDef();

                        // Set the source for the image file
                        acRasterDefNew.SourceFileName = strFileName;

                        // Load the image into memory
                        acRasterDefNew.Load();

                        // Add the image definition to the dictionary
                        acImgDict.UpgradeOpen();
                        acImgDefId = acImgDict.SetAt(strImgName, acRasterDefNew);

                        acTrans.AddNewlyCreatedDBObject(acRasterDefNew, true);

                        acRasterDef = acRasterDefNew;

                        bRasterDefCreated = true;
                    }

                    // Open the Block table for read
                    BlockTable acBlkTbl;
                    acBlkTbl = acTrans.GetObject(acCurDb.BlockTableId, OpenMode.ForRead) as BlockTable;

                    // Open the Block table record Model space for write
                    BlockTableRecord acBlkTblRec;
                    acBlkTblRec = acTrans.GetObject(acBlkTbl[BlockTableRecord.ModelSpace],
                                                    OpenMode.ForWrite) as BlockTableRecord;

                    // Create the new image and assign it the image definition
                    using (RasterImage acRaster = new RasterImage())
                    {
                        acRaster.ImageDefId = acImgDefId;
                        //image = acRaster;
                        // Use ImageWidth and ImageHeight to get the size of the image in pixels (1024 x 768).
                        // Use ResolutionMMPerPixel to determine the number of millimeters in a pixel so you 
                        // can convert the size of the drawing into other units or millimeters based on the 
                        // drawing units used in the current drawing.

                        // Define the width and height of the image
                        Vector3d width;
                        Vector3d height;

                        // Check to see if the measurement is set to English (Imperial) or Metric units
                        //if (acCurDb.Measurement == MeasurementValue.English)
                        //{
                        //width = new Vector3d((acRasterDef.ResolutionMMPerPixel.X * acRaster.ImageWidth) / 25.4, 0, 0);
                        //height = new Vector3d(0, (acRasterDef.ResolutionMMPerPixel.Y * acRaster.ImageHeight) / 25.4, 0);
                        //}
                        //else
                        //{
                       
                        if (iter.width < 0)
                        {
                            iter.width = acRaster.ImageWidth;
                        }
                        if (iter.height < 0)
                        {
                            iter.height = acRaster.ImageHeight;
                        }
                        if (iter.x < 0)
                        {
                            iter.x = 0;
                        }
                        if (iter.y < 0)
                        {
                            iter.y = 0;
                        }
                        width = new Vector3d(iter.width, 0, 0);
                        height = new Vector3d(0, iter.height, 0);
                        //}

                        // Define the position for the image 
                        Point3d insPt = new Point3d(iter.x, iter.y, 0.0);

                        // Define and assign a coordinate system for the image's orientation
                        CoordinateSystem3d coordinateSystem = new CoordinateSystem3d(insPt, width, height);
                        acRaster.Orientation = coordinateSystem;

                        // Set the rotation angle for the image
                        acRaster.Rotation = 0;

                        // Add the new object to the block table record and the transaction
                        iter.ID = acBlkTblRec.AppendEntity(acRaster);
                        acTrans.AddNewlyCreatedDBObject(acRaster, true);

                        // Connect the raster definition and image together so the definition
                        // does not appear as "unreferenced" in the External References palette.
                        RasterImage.EnableReactors(true);
                        acRaster.AssociateRasterDef(acRasterDef);

                        if (bRasterDefCreated)
                        {
                            acRasterDef.Dispose();
                        }
                    }
                }
                // Save the new object to the database
                acTrans.Commit();

                // Dispose of the transaction
            }
        }
        [CommandMethod("OutputMap")]
        public void OutputMap()
        {
            //Point3d p = line1.StartPoint;

            Database acCurDb;
            acCurDb = Application.DocumentManager.MdiActiveDocument.Database;
            // Get the image dictionary
            ObjectId acImgDctID = RasterImageDef.GetImageDictionary(acCurDb);

            // Check to see if the dictionary does not exist, it not then create it
            if (acImgDctID.IsNull)
            {
                acImgDctID = RasterImageDef.CreateImageDictionary(acCurDb);
            }
            using (Transaction acTrans = acCurDb.TransactionManager.StartTransaction())
            {
                Polyline Rect = WorldEdgeRect.GetObject(OpenMode.ForRead) as Polyline;
                Point2d pmax = Rect.GetPoint2dAt(2);
                AllMap[CurrentWorldMap].width = pmax.X;
                AllMap[CurrentWorldMap].height = pmax.Y;
                foreach (var Map in AllMap[CurrentWorldMap].maps[CurrentLayer])
                {
                    if (!Map.ID.IsNull)
                    {
                        //acRasterDef = acTrans.GetObject(ID, OpenMode.ForWrite) as RasterImageDef;
                        //}
                        //RasterImage image = new RasterImage();
                        //image.ImageDefId = ID;
                        //Point3d p2 = image.Position;
                        RasterImage image = null;
                        image = Map.ID.GetObject(OpenMode.ForRead) as RasterImage;
                        if (image != null)
                        {
                           
                            Point3d p = image.Orientation.Origin;
                            Map.x = p.X;
                            Map.y = p.Y;
                            Map.width = image.Width;
                            double width2 = image.ImageWidth;
                            Map.height = image.Height;
                            double height2 = image.ImageHeight;
                        }
                        
                    }  
                }
                WriteMapData();
            }
           
        }
    }
}
