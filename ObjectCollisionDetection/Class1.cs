using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace ObjectCollisionDetection
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.ReadOnly)]

    public class Class1 : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Get the handle of the current document.
                UIDocument uidoc = commandData.Application.ActiveUIDocument;
                Document doc = uidoc.Document;

                //Collecting IDs of family-instances loaded by us like furniture or other objects
                FilteredElementCollector collector1 = new FilteredElementCollector(doc);
                ICollection<ElementId> familyInstanceElement = collector1.OfClass(typeof(FamilyInstance)).ToElementIds();

                //Collecting id of floor to remove from the objects need to be check for collision
                FilteredElementCollector collector2 = new FilteredElementCollector(doc);
                ICollection<ElementId> floorId = collector2.OfClass(typeof(Floor)).ToElementIds();

                string collisionReport = null;
                int count = 0;

                List<ElementId> elementIds = new List<ElementId>();

                //It takes the elements one by one and check for the collision occurs or not
                foreach (ElementId familyInstanceId in familyInstanceElement)
                {
                    if (!elementIds.Contains(familyInstanceId))
                    {
                        Element familyInstance = doc.GetElement(familyInstanceId);

                        //Taking the boundingbox for collision detection
                        BoundingBoxXYZ boundingBoxXYZ = familyInstance.get_BoundingBox(doc.ActiveView);
                        Outline outline = new Outline(boundingBoxXYZ.Min, boundingBoxXYZ.Max);

                        //Creating filter of collision detection
                        BoundingBoxIntersectsFilter boundingBoxIntersectsFilter = new BoundingBoxIntersectsFilter(outline);

                        //Getting all elements except floor
                        FilteredElementCollector filterElements = new FilteredElementCollector(doc, doc.ActiveView.Id).Excluding(floorId);

                        ICollection<ElementId> currentElement = new List<ElementId>();
                        currentElement.Add(familyInstanceId);

                        //Excluding current element and apply filter of collision
                        filterElements.Excluding(currentElement).WherePasses(boundingBoxIntersectsFilter);

                        foreach (Element intersectingElement in filterElements)
                        {
                            //Checks whether the object is close to wall or not
                            if (intersectingElement.Category.Name == "Walls")
                            {
                                ++count;
                                collisionReport += $"{count}. (ID:{intersectingElement.Id}) is very close to wall\n";
                            }
                            else
                            {
                                //Adding note if object collide with each other
                                elementIds.Add(intersectingElement.Id);
                                ++count;
                                collisionReport += $"{count}. (ID: {familyInstance.Id}) and (ID: {intersectingElement.Id})\n";
                            }
                        }
                    }
                }
                if (collisionReport == null)
                {
                    TaskDialog.Show("Objects Collision Report", "Objects are not collide at all");
                }
                else
                {
                    TaskDialog.Show("Objects Collision Report", $"Collision occurs at {count} place between following elements:\n" + collisionReport);
                }
            }
            catch (Exception e)
            {
                message = e.Message;
                return Result.Failed;
            }
            return Result.Succeeded;
        }
    }
}
