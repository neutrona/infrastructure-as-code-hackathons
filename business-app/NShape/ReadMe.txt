====================================================================================================
Known issues:
====================================================================================================

- Transparent bitmap images with 8 bit color depth are not rendered correctly when compiling in 
  x64/AnyCPU configuration on x64 OS.


====================================================================================================
Changes in 2.2.1:
====================================================================================================
- Interface Change: Definition of the abstract StyleCollection base class changed from 
    public abstract class StyleCollection<TStyle> where TStyle : class, IStyle
  to 
    public abstract class StyleCollection<TStyle, TStyleInterface> : IEnumerable<TStyleInterface>
		where TStyle : class, TStyleInterface 
		where TStyleInterface : class, IStyle
  due to the fact that the implementation of the enumerator moved from the specific style collections
  to the base class which in turn wraps the enumerator of the internal list (see below).
- Bugfix: Changing the StyleCollection iterator's implementation (see above) fixes a bug where you 
  can change the collection's key while iterating through the collection. Doing so will now raise an
  InvalidOperationException.
- Bugfix: EmbeddedImageWriter now calls all IEntity.SaveFields and IEntity.SaveInnerObjects methods
  with the library's version instead of the base repository version.
- Bugfix: Moving shapes with arrow keys will no longer focus the scroll bars (which will break 
  scrolling and zooming behavior)
- BugFix: The display component no longer runs into an exception when pressing F2 while an 
  ICaptionedShape without captions is selected.
- Bugfix: Selecting shapes inside a group (or double clicking overlapping shapes) does no longer
  cause the Display.ShapesSelected event to be fired multiple times when OverlappingShapesAction
  is set to OverlappingShapesAction.Cycle.
- Bugfix: RegularPolygone now draws itself in the correct size after loading from repository
- BugFix: Fixed a NullReferenceException when dragging a group of shapes loaded from XML repository
- Bugfix: Saving picture shapes with to XML store with with ImageLocation == Embedded on caused 
  a NullReferenceException.
- Bugfix: Image directory was not deleted when deleting an XML repository (ImageLocation == Directory).
- Bugfix: Fixed a backwards compatibility issue in XML store: 
  Files created with NShape versions <= 1.0.3 could not be read with NShape versions >= 1.0.4.
- Improvement: StyleCollections will now handle renaming styles and maintain their name based indexer 
  automatically in this case.
- Improvement: Improved drawing performance of the diagram sheet's line grid noticeably.
- Improvement: When selecting a shape and dragging it away within the double click detection time 
  will now be handled as expected.
  
====================================================================================================
Changes in 2.2.0:
====================================================================================================
- Interface Change: IDisplayService.NotifyBoundsChanged is obsolete now. 
  Use the Resized event of the diagram instead (see below).
- Interface Change: Changed several event args classes:
    * EventArgs class ShapeMouseEventArgs (unused) was deleted.
	* EventArgs class ShapeEventArgs (unused) was renamed to ShapesEventArgs
	* EventArgs class DiagramShapeEventArgs (unused) was renamed to DiagramShapesEventArgs
	* EventArgs class ShapeEventArgs was added.
	* EventArgs class DiagramPresenterShapeEventArgs was added
- Extended Interface: IDiagramPresenter interface is now derived from ISynchronizeInvoke in order to 
  enable thread synchronized timer events (when using System.Timers.Timer's property 
  "SynchronizingObject").
- Extended Interface: Added IDisplayPresenter.CloseCaptionEditor(bool applyChanges)
- Extended Interface: Added three events to IDisplayPresenter:
	event EventHandler<DiagramPresenterShapeEventArgs> ShapeMoved;
	event EventHandler<DiagramPresenterShapeEventArgs> ShapeResized;
	event EventHandler<DiagramPresenterShapeEventArgs> ShapeRotated;
- Extended Interface: Added boolean property CanModifyVersion to the IRepository interface.
  Specifies whether the current repository implementation supports upgrading the load/save version of 
  the repository.
- Extended Interface: Added Method UpgradeVersion to the IRepository interface which will upgrade the 
  load/save version of a repository (or throw an exception if upgrading is not supported).
- Extended Interface: Added boolean property CanModifyVersion to the Store base class.
  The shipped XmlStore will support upgrading load/save version whereas the shipped AdoNetStore will 
  not.
- Extended Interface: Added the following members to the XmlStore class (see new features for details):
	bool LazyLoading { get; set; }
	ImageFileLocation ImageLocation { get; set; }
- Extended interface: Added four events to the Diagram class:
	event EventHandler<ShapeEventArgs> ShapeMoved;
	event EventHandler<ShapeEventArgs> ShapeResized;
	event EventHandler<ShapeEventArgs> ShapeRotated;
	event EventHandler Resized;
- Extended interface: Base class "Shape" was extended by the following protected members in order 
  to enable custom collection implementations for the Shape.Children collection.
	protected ShapeAggregation ChildrenCollection { get; }
		Provides access to the collection that stores and manages the shape's child shapes. The 
		collection is null by default and will be created when needed and deleted when not needed 
		any more.
	protected virtual ShapeAggregation CreateChildrenCollection(int capacity);
		Creates an instance of the children collection with the given capacity. Override this method
		if a custom implementation of the children collection is needed.
- Extended interface: Added two methods to the ICaptionedShape interface:
	bool GetCaptionIsVisible(int index)
	void SetCaptionIsVisible(int index, bool isVisible)
- Extended Interface: For implementing the new methods above, the Caption class got a new property
	bool IsVisible { get; set; }
- Extended Interface: Base class Tool has now a property DoubleClickTime that takes the interval (in ms)
  used for interpreting two subsequent clicks as double click.
- Extended Interface: Added three protected virtual methods to ReposioryWriter Base class: 
    DoWriteModelObject
    DoWriteStyle
    DoWriteTemplate
- Extended Interface: Added constructor overloads to the DelegateMenuItemDef class
- Interface Change: Data type of Geometry.Signum changed from int to sbyte.
- Interface Change: Moved the following (never implemented) methods from class CachedRepository to 
  class Project and implemented them:
	string GetXml() 
	void WriteXml(Stream stream)
	void OpenXml(Stream stream)
- New Feature: XmlStore now supports partial loading for improved performance. Partial loading is 
  disabled by default (for backwards compatibility) and can be enabled using the XmlStore.LazyLoading
  property:
  When loading diagrams, the diagram instance is loaded without contents. The diagram contents have 
  to be loaded explicitly by calling the IRepository.GetDiagramShapes method.
  Make sure that all GetDiagramShapes was called for all diagrams before saving.
- New Feature: XmlStore provides a new property ImageLocation which specifies whether images are 
  stored in a seperate directory (as it was until now) or whether images are embedded into the XML
  file as base64 encoded string.
  When loading an existing XML file, the ImageLocation setting of the file will be determined and used 
  until it the XmlStore is closed or the ImageLocation is overridden by setting the ImageLocation 
  property while the XmlStore is open.
- New Feature: NShapeDesigner implements the new 'Upgrade Version' feature (see Interface extensions 
  above).
- New Feature: NShapeDesigner implements the new 'Use Embedded Images' feature (see above).
- New Feature: NShapeDesigner implements Project.GetXml (see above) with menu item "Export Repository 
  to Clipboard (XML)" located in the "File" menu.
- New Feature: NShapeDesigner implements Project.OpenXml (see above) with menu item "Import Repository 
  from Clipboard (XML)" located in the "File" menu.
- Improvement: XmlStore has an additional constructor that takes a stream instead of a directory and 
  a file extension. If the XmlStore was constructed using this constructor, the stream will be used 
  for loading/saving for the lifetime of the XmlStore.
- Improvement: Inserting diagrams with duplicate names in the repository will now raise an exception.
- Improvement: You can now select "None" as color for line caps. In this case, the line cap will be 
  filled with the color of the line.
- Improvement: Updated all demo projects to version 5 (using embedded images).
- Improvement: Updated documentation to reflect all interface changes of the last few versions and 
  added several yet undocumentated classes.
- Changed Behavior: The CachedRepository now allows the assignment of a store while repository is 
  open in case that no store exists yet.
- Changed behavior: NShapeDesigner no longer asks for a file name when creating a new XML project. 
  Instead, no store is created in this case in the first place. It will be created when saving the 
  project for the first time.
- Changed Behavior: Holding down the control key while moving a shape means now 'Toggle snapping to
  grid' instead of 'Do not snap to grid'.
- Changed Behavior: When clicking on overlapping shapes (with OverlappingShapesAction != None), the 
  selection of the next/topmost shape will be deferred until the double click detection time elapses. 
- Changed Behavior: Tools never received MouseUp events with clicks > 1 because WinForms controls reset
  the click count before raising the MouseUp event of the double click's second click. 
  In Addition to that, multi-clicks are now supported (tripple-click, quadruple-click, etc) as the click
  count will not be reset after a double click (as WinForms controls do).
- Bugfix: Clicking the save button in NShapeDesigner while editing the caption of a shape will no 
  longer cause an exception.
- Bugfix: NShape components will now remove all registered event handlers propertly.
- Bugfix: Undoing "ungroup" on a rotated group now reverts the rotation of the group members correctly
- Bugfix: When creating linear shapes, the connection points near the mouse cursor are now drawn.
- Bugfix: Constructor AggregatedCommand(IRepository repository, IEnumerable<ICommand> commands) no 
  longer raises a NullRegerenceException.
- Bugfix: XmlRepositoryReader.ReadDate() did not advance the underlying XmlReader to the next attribute.
- Bugfix: NShape 1.0.0 to 1.0.3 "Repository Version 2" stored templates including their title.  
  NShape 1.0.4 to NShape 2.1.1 neither store nor read the template's title for version 2 repositories, 
  thus breaking compatibility with old repository files. 
  As NShape 1.0.4 also introduced the "Repository Version 3" storage format (default for new projects), 
  we repaired the compatibility issue so you can now open "Version 2" repositories created with 
  NShape 1.0.0 - 1.0.3 but not "Version 2" repositories created with NShape 1.0.4 - 2.1.1.
  If you need to open old version 2 files, please insert an attribute "title" between the "name" and 
  the "description" attribute of all template tags:
  <template id="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx" name="xxxxxxxx" description="">
  becomes
  <template id="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx" name="xxxxxxxx" title="" description="">
- Bugfix: The project component no longer accepts version 1 as minimum repository version. Version 2 is
  the correct minimum load/save version as NShape 1.0.0 was released creating version 2.
- Bugfix: XmlStore did never call "SaveInnerObjects" method of model objects.
- Bugfix: Added a workaround for an issue that causes an OutOfMemoryException in the underlying GDI+ 
  Flat API functions when trying to draw lines with intersecting line caps.

====================================================================================================
Changes in 2.1.1:
====================================================================================================
- Bug Fix: Saving repositories version 3 and 4 no longer causes an IndexOutOfRangeException.
- Improvement: Added descriptive comments to all demo programs and the tutorial projects.

====================================================================================================
Changes in 2.1:
====================================================================================================
- Interface Change: Classes PropertyMappingIdAttribute and RequiredPermissionAttribute have been sealed
- Interface Change: 
    protected Point[] PlanarShapeBase.controlPoints is now private
	protected void PlanarShapeBase.ResizeControlPoints(int length) has been added
  You can resize the control points array using the new method and access its points using the 
  public property ControlPoints. This addresses the problem that controlPoints and ControlPoints did only 
  differ in casing, making it impossible for VB.NET programmers to implement their own PathBasedShapes 
  without changing the source code.
- Extended Interface: Added a new method to the Dataweb.NShape.Shape interface base class and a default 
  implementation to the ShapeBase class:
	public abstract bool CanConnect(ControlPointId ownPointId, Shape otherShape, ControlPointId otherPointId);
  This method can be used for implementing custom connection behavior like accepting a limited number of 
  connections or accepting only connections of a certain shape type.
- Extended Interface: Added string property "Data" to the Dataweb.NShape.Shape interface class. It can be 
  used for storing user defined string data that will be saved to and loaded from the repository (in 
  contrast to the Tag property).
- Extended Interface: Added string property "Description" to the Dataweb.NShape.Project class.
  saved to and loaded from the repository (in contrast to the Tag property).
- Extended Interface: Added a new method to the shape collection interface:
	public interface IShapeCollection 
		int GetZOrder(Shape shape)
- Extended Interface: Added an overloaded version of Project.IsValidLibrary(...) that returns the reason 
  why the assembly is considered as invalid library assembly (as out parameter).
- Extended Interface: Access modifiers of the following members of the SelectionTool class changed:
	SelectionTool.FindPreviewOfShape(Shape shape) is now public
	SelectionTool.FindShapeOfPreview(Shape previewShape) is now public
	SelectionTool.Action is now a protected enum.
- Extended Interface: Published constants for the control point ids of most planar shape classes 
  through the nested class ControlPointIds that provides all suitable constants.
  Derived shapes that change the number of available ControlPoints or their id's have to override this
  class.
- Extended Interface: Display.EnsureVisible now has an overloaded version that accepts a margin
- Extended Interface: IDiagramPresenter.AcitveTool gives access to the currently used Tool of the diagram 
  presenter

- Property Rename: Display.ShowGrid was renamed to Display.IsGridVisible (ShowGrid is still available but  
  marked as obsolete)
- Property Rename: Display.CurrentTool was renamed to Display.ActiveTool (CurrentTool is still available but 
  marked as obsolete)

- Changed behavior: FreeTriangle shapes throw an ArgumentException in CalculateRelativePosition in case
  the given point is not part of the shape. This shape type does not support points outside the shape bounds
- ChangedBehavior: Methods taking an index parameter as argument will no longer throw an 
  "IndexOutOfRangeException" but an "ArgumentOutOfRangeException" (except the style collection's indexer)
- ChangedBehavior: Backup files created by the XmlStore component now preserve the original file extension 
  by adding the backup file extension instead of replacing it.
- ChangedBehavior: IDiagramPresenter.EnsureVisible no longer add a margin to the given parameters.
  There are new overloaded versions of these methods in the Display control offering a margin parameter.

- New Feature: The SelectionTool provides a new property "OverlappingShapesAction" that specifies
  the action when clicking on overlapping shapes. The default value is "Cycle" so the default behavior 
  does not change. Alternative values: Do not change the selection ("None") or select the topmost 
  shape ("Topmost").
- New Property: The XmlStore now has a string property BackupFileExtension which allows to customize 
  the file extension of automatically created backup files.
- New Property: Display.IsSheetVisible allows to hide the diagram sheet (including its shadow).
  
- Improvement: ImageBasedShape has now (very basic) implementations of CalculateRelativePosition and 
  CalculateAbsolutePosition
- Improvement: Connected linear shapes can now be rotated.
- Improvement: Resizing multi segment polylines in "MaintainAspect" mode improved (also affects connected
  multi segment lines).
- Improvement: Setup now allows to specify the installation path for the source code

- Bugfix: Corrected an error in AutoDisconnectShapesCommand that occured when deleting a shape with 
  model object.
- Bugfix: Geometry.LineIntersectsLine() returned true for parallel lines with opposing direction 
  vectors
- Bugfix: DoubleClick with SelectionTool on overlapping shapes no longer results in selecting the 
  shape below the double-clicked shape.
- Bugfix: Display.DiagramChanging event is now raised *before* bounds and layers are reset to defaults
- Bugfix: Methods UnselectShape() and UnselectAll() do not raise any longer a Display.ShapesSelected 
  event if no shapes are selected.
- Bugfix: Display.ShapesInserted is now raised before Display.ShapesSelected.
- Bugfix: AggregatedCommand caused endless self-recursion if a necessecary permission was not granted.
- Bugfix: When inserting a shape into the repository, the display component fired the ShapesSelected 
  event before the ShapesInserted event.
- Bugfix: Pasting a shape fired multiple ShapesSelected events 
- Bugfix: Removed a Debug Assertion when copying diagrams as metafile to the clipboard. This assertion 
  failed when copying larger diagrams or diagrams with complex graphics due to timing problems of
  asynchroneous calls.
- Bugfix: NShapeDesigner's "Select All" menu items now use Display.SelectAll() instead of 
  Display.SelectShapes(Display.Diagram.Shapes, false)
- Bugfix: LabelBase did not calculate the correct number of cells for the diagram's spatial index when
  connected. This caused "Entity Not Found in Repository" exceptions when trying to delete these 
  labels in case the glue point was in another spatial index cell as the label's body.
- Bugfix: LineShapeBase did not check whether the cap's shape points were calculated before performing
  an intersection calcuation in StartCapIntersectsWith and EndCapIntersectsWith which resulted in an 
  ArgumentNullException.
- Bugfix: Events Display.KeyDown, Display.KeyUp and Display.KeyPress now take EventArgs.Handled into
  account correctly.
- Bugfix: The following shape classes did not calculate the correct relative and/or absolute position:
    * ShapeGroup
    * RegularPolygon
    * ImageBasedShape
- Bugfix: Connections between grouped (or aggregated) shapes were not stored to XML
- Bugfix: Connected shapes inside groups or composite shapes were moved to the wrong positions when
  moving, rotating or resizing the group/composite shape
- Bugfix: Hit test did not work correctly when clicking a line cap of type "OpenArrow"
- Bugfix: Trying to open LayerPresenter's content menu caused a ArgumentNullException when the attached 
  display had no diagram.
- Bugfix: When creating the SelectionTool, the ToolBoxController no longer raises the ToolSelected event 
  before the ToolAdded event.
- Bugfix: Corrected example in documentation "Programmer Tasks > Customizing Context Menus > Integrate 
  NShape Commands in your Own Context Menus": The code of the lambda expression now extracts the 
  MenuItemDef from the context menu item and executes it.
- BugFix: SoftwareArchitecture.EntityShape now handles deleting items of string array property 
  "ColumnNames" correctly when assigning an other string array to this property. Throws an exception if 
  connected control points should be removed.
- BugFix: A PolyLine connected to the center point of a RegularPolygone did not calculate the preview
  shape's position correctly.
- BugFix: XmlStore now deletes the existing (old) XML file (and its image directory) when setting
  XmlStore.BackupGenerationMode to BackupFileGenerationMode.None.
- BugFix: Moving connected lines that are grouped or selected together with their partner shapes will no 
  longer move their vertices to strange positions (REQ 20411)
- BugFix: When deleting a Shape without its attached model object (delete shape only), the model object
  will now be correctly detached from the shape.
- BugFix: When trying to save a new project over an existing one, there is no longer an "already exists"
  error message 


====================================================================================================
Changes in 2.0.3 (Beta 4):
====================================================================================================
- Changed Implementation Rule: The implementation rule for the Shape.Clone() method has changed. The 
  shape clone has to instantiated using the source shape's Template.
  See documentation for sample code.

- Changed Behavior: When calling ToolSetController.Clear(), a ToolSelected event is raised (with
  the eventArgs.Tool set to null/Nothing). Removed ArgumentNullExceptions when assigning null/Nothing 
  to the SelectedTool property or calling SelectTool(null).
- Changed Behavior: Exceptions thrown in the OnMouseXXX and OnKeyXXX methods of the display will no 
  longer be swallowed silently but re-thrown (after canceling the current tool action).
- Changed Behavior / New Feature: Selected shapes can be moved with the arrow keys. If no shapes are 
  selected, the diagram will scroll. Shift key is used to speed up movement.
- Changed Bahavior / New Feature: Deselection of selected shapes using a selection frame: If all 
  shapes inside the frame are already selected, they will be deselected.
- Bugfix: The shape's template was not copied when calling Shape.Clone() (see changed implementation 
  rule) 
- Bugfix: The ModelObject got lost when cutting and pasting the shape using DiagramSetController.Cut
  and DiagramSetController.Paste, the appropriate context menu commands or keyboard shortcuts.
- Bugfix: Improved workaround for the strange GDI+ issue concerning lines with custom line caps
- Bugfix: When clicking or double clicking on overlapping shapes, a ShapeClick / ShapeDoubleClick 
  event was raised for all shapes containing the clicked coordinate.
  The event will now be raised for the selected shape at the clicked coordinates (if there is any)
  or the topmost of the overlapping shapes.
- Bugfix: When deleting shapes, the event args of the ShapesRemoved event did not contain any shapes.
- Bugfix: When clearing the tools of the ToolSetController, the selected tool and the default tool's 
  properties were not cleared, containing still a reference to the old tool (supposed to be deleted).
  The DiagramSetController.ActiveTool also referenced the tool supposed to be deleted.
- Bugfix: When adding a new Tool to the ToolSetController as "default tool", the linked 
  DiagramSetController's ActiveTool property will be updated.
- Bugfix: A NullReferenceException was sometimes thrown when selecting shapes (with caption)
- Bugfix: An OverflowException was caused when saving/loading ParagraphStyle with Alignment 
  "BottomCenter". 
  Changed PropertyDefinition of ParagraphStyle.Alignment from Byte to Short in order to avoid this
  issue. 
- Bugfix: Shapes were not invalidated after changing their template if they were grouped.
- Bugfix: OnScroll messages sent to the directly to the display control (as some touchpad drivers do
  when scrolling with finger gestures) are now handled correctly. 
- Bugfix: Fixed bug in CircularArc while recalculating radius point position (while connected shape-
  to-shape)
- Improvement: Removed assertion failure when trying to connect a line to a grouped shape.
- Improvement: Adding multiple Layers will no longer cause the LayerListView to redraw after each 
  added item.
- Improvement: Added Tooltips for the status bar controls.


====================================================================================================
Changes in 2.0.2 (Beta 3):
====================================================================================================
- New Sample Program: "ModelMapping Demo" that shown how to visualize states of business objects 
  implementing the IModelObejct interface
- Buxfix: LayerCollection.Add() inserts layer objects at the correct position if a LayerId is already set 
  in the layer object.
- Bugfix: Added a workaround for a strange issue concerning lines with custom line caps:
  If a 1-segment line is drawn and the line has exactly the same length as the sum of both cap's 
  BaseInset, an OutOfMemoryException is thrown sometimes (not always). The exception is thrown in 
  DrawLines and all efforts to trace down the cause of this issue came to nothing.
- Bugfix: Some NShape dialogs did not close when clicking "OK" or "Cancel" if not shown modal.
- Bugfix: CircularArc added menu command "Remove Point" even if no vertex was right-clicked
- Bugfix: Display threw a NullReferenceException when calling LoadDiagram or CreateDiagram without 
  setting a DiagramSetController first
- New Feature: XMLStore.BackupGenerationMode allows to switch off the automatic generation of a *.bak file
- Changed Behavior: When selecting overlapping shapes, the shapes will not enter the "Edit caption mode"
  when an other shape is under the caption. You can still enter "Edit caption" mode by pressing F2 key.
- Improvement: Resizing aggregated shapes now works better for shapes with resize movement restrictions
  (such as Circles, Squares, CustomizableImageShapes, etc)
- Improvement: NShapeDesigner will no longer ask "Save changes?" for the project created on startup
- Improvement: NShapeDesigner (XML) projects have "*.nspj" as file extension by default now. 
  Documentation and sample code was updated accordingly.


====================================================================================================
Changes in 2.0.1 (Beta 2):
====================================================================================================
- Buxfix: IRepository.Insert(Shape shape) and IRepository.Insert(IEnumerable<Shape> shapes) no longer
  insert shape connections.
- Bugfix: Several context menu items did show up although the required permission was not granted
- Bugfix: Display did not reset the visible and active layers when changing the diagram
- Bugfix: NullReferenceException when pressing Del key and no shape was selected
- Changed Behavior: LayerCollection.Find("") no longer throws an exception.
- Improvement: Display does not reset the displayed area of the diagram when hidden
- Improvement: Export dialog adds file extension when not specified
- Improvement: Export dialog asks whether to overwrite a file after the path changes
- Improvement: Export dialog updates the file extension when changing the file format (if a non-standard 
  file extension was specified, the user will be asked whether to update the extension)
- Documentation: Updated sample code in some chapters of the "Basic Tutorial" to NShape 2.0 code


====================================================================================================
Changes in 2.0.0 (Beta):
====================================================================================================

New / Changed / Extended Interfaces:
	public interface ISecurityManager
	public interface ISecurityDomainObject
	public interface IModelObject : ISecurityDomainObject
	public interface ILayerCollection : ICollection<Layer> 
	public interface IRepository
	public interface ICommand
	public interface IModelMapping
  See documentation of these interfaces for details.

Changes in base classes:
- Removed class "DefaultSecurity"
- New method in class Shape: HasStyle(IStye style) - return true if the given Style is used by the 
  shape.
- Shape.NotifyStyleChanged now uses HasStyle which has to be implemented. Therefore, NotifyStyleChanged 
  was made protected internal.
- New Method: Project.RemoveLibrary
- New Property: Project.AutoLoadLibraries (see "Behavior Changes")
- New Parameter for methods Project.AddLibrary, Project.AddLibraryByName and Project.AddLibraryByFilePath
  (see "Behavior Changes")
- Removed History.CommandExecuted event because it was obsolete. Use CommandsExecuted event instead.
- Several Methods of various classes were renamed. The old versions are marked as obsolete.
- PointerTool was renamed to SelectionTool. PointerTool is now marked as obsolete.
- New method in LinearShapeBase for calculating the bounding rectangle of the line (without line caps): 
  protected abstract Rectangle CalcBoundingRectangleCore(bool tight)
- Design class now has a Title property.

Namespace Changes:
- All collection classes are now in Dataweb.NShape.Collections
- All command classes are now in Dataweb.NShape.Commands
- Shape class, Template class and Tool classes are now in Dataweb.NShape
- Moved several classes from Dataweb.NShape.Advanced to Dataweb.NShape (or vice versa)

Behavior Changes:
- Project will no longer unload all libraries when opening a project. Libraries added by the application
  without the "unloadOnClose" option will not be unloaded. See Documentation for details.
- Project will no longer load needed libraries automatically (potential security issue). 
  In order to restore this behavior, set the project's AutoLoadLibrary property to true. See Documentation 
  for details.
- IRepository.GetDiagram and IRepository.GetDiagrams no longer load the diagram with all shapes but 
  the diagram object only (SQL Repository only).
- Therefore you have to call IRepository.GetDiagramShapes(Diagram diagram) when fetching the diagram 
  from Respository and not displaying it with the WinformsUI.Display component.
  Note that DiagramSetController.OpenDiagram and Display.OpenDiagram/LoadDiagram load the diagram 
  including all of its shapes.
- IRepository: Insert/Update/Delete/Undelete methods will only insert/update/delete/undelete the 
  given object(s) but no child- and contained objects.
- IRepository: InsertAll/DeleteAll/UndeleteAll will insert/delete/undelete the given objects(s) 
  and all their child- and contained objects.
- Template.GetTerminalName(TerminalId.Generic) no longer throws an ArgumentOutOfRangeException
- IsAllowed of an empty collection of shapes (where no domain name is obtainable) now checks if the 
  required permission is granted for at least one domain and returns this result.
- Shape.CalculateBoundingRectangle returns Geometry.InvalidRectangle if calculation of a bounding 
  rectangle is not possible. Check with the Geometry.IsValid method.
- Permissions are now split into Domain Permissions and General permissions. See documentation and for 
  details.

New Features:
- New CapStyle shapes available, all non-sizable (simple line-endings)
- Display.CopyImageToClipboard() copies the selected shapes (or the diagram if no shapes are selected) 
  as image to the clipboard.
- Shape libraries can be removed now
- NShape designer: Ctrl+A shortcut for "Select all shapes"
- New Shape: Regular Polygone (3 to Int32.MaxValue control points)
- New Shape: Free Triangle
- New Shape: Rectangular line
- Insertable/Deletable connection points (not vertices) for line shapes
- The AdoNet Store now supports partial loading (see behavior change of Repository.GetDiagram)
- Libraries can now be unloaded from the project (if not used any longer).
- Renamed line cap style "Arrow" to "ClosedArrow"
- New line cap styles: OpenArrow, Round (Default style), Flat and Peak. Round, Flat and Peak have
  no size.
- Dragging resize points with pressed Shift key will resize the shape with constant aspect ratio
  if shape supports MoveControlPoint with ResizeModifier.MaintainAspect flag set.
- Dragging resize points with pressed Ctrl key will resize the shape to both directions if shape 
  supports MoveControlPoint with ResizeModifier.MirroredResize flag set.

Improvements:
- Improved Diagram Export Dialog: 
  * A file extension will be added when not specifying one
  * If the file name's extension will adjust when switching to another image format (except when the 
    file has a non-standard extension)
  * Calling the export dialog with an empty diagram no longer produces an error message
- Improved copying the size of a shape when calling Shape.CopyFrom() other shape type
- Improved Shape.Fit() when the shape has children that exceed the parent's bounds
- Improved hit testing of GeneralShapes.ThickArrow specific implementation of ContainsPointCore())
- SoftwareArchitectureShapes.EntitySymbol: When editing a caption, all other captions will 
  not be rendered transparent any longer
- SoftwareArchitectureShapes.EntitySymbol: Fixed bug in hit test of the last caption
- Delete Template will 
- Re-activated ResizeModifiers for resizing shapes
- MoveControlPointTo/MoveControlPointBy did not always return false if the point could not be moved 
  to the specified position
- Before a style or a template can be deleted, the repository is checked if it is still used by any 
  object (objects that are currently not loaded will be checked too)
- AdoNetStore: Connection to the database will not be opened/closed for every nested/recursive call
- Improved copy/paste shapes: Pasted shapes will be placed more accurate at the position where they
  are expected
- Improved error message when trying to load a repository with a version that is not supported.
- LibraryManagementDialog implements removing libraries now. Changes are now performed when closing 
  the dialog with OK
- NShape Designer: New menu item "Tools" / "Test Data Generator" for creating diagrams
- Saving a "PictureShape" displaying an emf image will now release the file lock of saved EMF file.
- NShape Designer will no longer load all diagrams with all shapes when opening a project. 
  Instead, it will create a tab for each diagram in the project. The diagrams will be loaded when 
  switching to the appropriate tab.
- NShape Designer does not load the diagram shapes when opening the project (if the Store supports 
  partial loading, e.g. the SQLStore). The shapes will be loaded when clicking the diagram's tab. 
- Closing many "NShape Designer" instances at once will no longer produce an error message.
- Demo Program �Shuffle Game� now works with Display.ShapeClick event instead of Display.MouseClick 
  event and uses more NShape specific methods for finding and moving shapes.
- LayerController now uses the new LayerEditCommand instead of the PropertySetCommand<Layer>, thus
  avoiding the usage of reflection
- Layouter Dialog deactivates its Undo/Redo buttons if there is nothing left to undo/redo
- XmlStore.DirectoryName now ofers designer support in VisualStudio's WinForms Designer (offers a 
  [...] button that opens a "Select Directory" dialog)
- NShape Designer: Zoom setting is now "per diagram".
- Demo project "Flow Chart" now uses sticked labels instead of loose text shapes
- Display now suports the following shortcut commands: Shift+Del (Cut), Shift+Ins (Copy) and 
  Ctrl+Ins (Paste)
- Exceptions thrown in Form_Load event handler will be catched by the base class, so we added 
  try/catch handlers in all Tutorial projects.
- New property PropertyController.PropertyDisplayMode: Properties can now be completly hidden or 
  displayed as read only if their RequiredPermission is not granted.
- Readonly Permissions can be granted via the SecurityAccess enum and the appropriate overloads of
  SetPermission(). Granting a Permission with SecurityAccess.View means that the property is displayed 
  as readonly. See documentation for details.

Bugfixes:
- Display no longer throws an exception when beeing placed on a control with transparent background 
  color, as it was the case with TabControl pages.
- When using more than one design, the design editor did not display the correct styles in 
  the property editor
- A call to Repository.Update() was missing in Tutorial "Templates"
- TerminalMapping on ReferencePoint were not stored in Repository
- TerminalMappings were not restored when loading from an SQL Repository
- Diagram.CreateImage(): Image background color was not working for �Classic EMF� format
- Cutting and pasting connected shapes no longer disconnects the shapes.
- Layouter Dialog: When having no shapes selected, switching between diagrams in NShape Designer now 
  correctly refreshes the layouted shapes.
- Setting a ContextMenuStrip for the ToolBoxListView component now works as expected and no longer
  resets the property to null.
- Pressing �Del� key when no shapes are selected no longer results in an exception.
- CircularArc consisting of 2 points now inserts itself correctly into the spatial index of the 
  diagram and therefore is now clickable as expected.
- CircularArc consisting of 2 points no longer throws an exception when right-clicking it
- RTU shape in SoftwareArchitectoreShapes: Changing the backgrond color now works as expected.
- Saving SQLRepositories containing SoftwareArchitectureShapes no longer produces an exception
- Assigning a new style to a template loaded from an SQLStore no longer results in an exception
- PictureShape with an empty picture (height or width 0) no longer produces an exception
- Scrolling with mouse wheel and scrolling with universal scroll (middle mouse button) now raises
  Scroll events as expected
- Deleted entities are no longer returned when by the Repository�s GetXXXs() methods.
- If trying to fetch a deleted object with the Repositoriy�s GetXXX(object id) method, an NShapeException 
  is now thrown.
- UniteRectangles(RectangleF a, RectangleF b) now behaves like UniteRectangles(Rectangle a, Rectangle b).
- When adding Vertices and deleting them in the same order they were created, undoing/redoing this changes
  no longer cause a wrong ControlPointId assignment for the added vertices.
- When editing a caption of an EntitySymbol shape (SoftwareArchitectureShapes library), clicking an
  other caption no longer renders the captions transparent
- Last caption of EntitySymbol shapes can now be removed as expected by context menu.
- Height of EntitySymbol's header text is now calculated correctly when containing a line break
- Styles can no longer be deleted if they are used in other styles or shapes (even if the shapes are not 
  loaded)


====================================================================================================
Changes in updated Documentation:
====================================================================================================

- Keywords for all topics (for index search)
- New Topic: Concepts / Shape Connections
- New Topic: Concepts / Security
- New Topic: Programmer Tasks / Controlling User Access / Defining User Roles and Permissions
- New Topic: Programmer Tasks / Controlling User Access / Assigning Security Domains
- New Topic: Programmer Tasks / Customizing Context Menus / Hiding Unauthorized Menu Items
- New Topic: Programmer Tasks / Customizing Context Menus / Extending Built-In Context Menus
- New Topic: Programmer Tasks / Customizing Context Menus / Integrate NShape Commands in your Own Context Menus
- New Topic: Programmer Tasks / Customizing the ToolBox / Automatic Template Creation
- New Topic: Programmer Tasks / Customizing the ToolBox / Loading a Template Project
- New Topic: Programmer Tasks / Customizing the ToolBox / Filling the Toolbox Manually
- Updated/Added Reference Topics for 
  Style Interfaces:
	IStyle Interface
	ICapStyle Interface
	ICharacterStyle Interface
	IColorStyle Interface
	IFillStyle Interface
	ILineStyle Interface
	IParagraphStyle Interface
  Style Collection Interfaces:
	IStyles Interface
	ICapStyles Interface
	ICharacterStyles Interface
	IColorStyles Interface
	IFillStyles Interface
	ILineStyles Interface
	IParagraphStyles Interface
  Misc Classes and Structs for drawing/styling
	ToolCache Class
	NamedImage Class
	TextPadding Struct  
  Shape Interfaces and Classes
	ILinearShape Interface
	IPlanarShape Interface
	ShapeType Class
  Persistency Interfaces and Classes:
	IRepositoryReader Interface
	IRepositoryWriter Interface
	Store Class
  Security and Access Interfaces and Classes
	ISecurityManager Interface
	ISecurityDomainObject Interface
	Permission Enumeration
	RoleBasedSecurityManager Class
	StandardRole Enumeration
- Deleted outdated topics
- Updated Layout
- Many minor changes and corrections


====================================================================================================
Changes in 1.0.7:
====================================================================================================

- Improved handling of multiple designs (SQL Repository only)
- Fixed several issues with the column commands of the shape SoftwareArchitectureShapes.EntitySymbol
- Improvement: Display now supports Ctrl+A shortcut for "Select all shapes"
- Marked class "DefaultSecurity" as [Obsolete]. It will be removed in the next version.
- Bugfix: Moving the mouse over a shape with text, selecting it and deleting it with "Del" did not 
  remove the highlighted caption bounds.
- Changed project structure (directory hierarchy) and build paths
- Bugfix: Events IDiagramPresenter.DiagramChanging and IDiagramPresenter.DiagramChanged were not 
  raised when assigning Display.Diagram directly
- Bugfix: Project will be closed now when an exceptions occures while loading
- Bugfix: LayerPresenter calls Clear() when project closes
- Bugfix: Internal Exceptions when executing tool actions on a display without diagram
- Bugfix: Displayed section of the diagram and scrollbar positions were wrong when changing the 
  diagram of the display component
- Bugfix: Item "More.." of the style selection comboBox no longer opens the design editor window in 
  background
- Bugfix: Some values are invisible after changing their values in design editor's property editor
- Bugfix: When renaming a line-, cap or character style, the selected style changes when committing 
  the property change
- Bugfix: A shape is not selectable by clicking if an other shape, that is hidden by layer settings, 
  covers it
- Bugfix: Diagram.CreateImage no longer raises a NullReferenceException when diagram has no 
  DisplayService
- Bugfix: NShape Designer's layout dialog now closes when closing the main window
- Bugfix: NShape Designer's layout dialog now closes when closing the project.
- Bugfix: NShape Designer's main menu items "Send to Back" / "Bring to Front" are functional now
- Improvement: All Dialogs of WinFormsUI will now close when Esc key is pressed
- Improvement: StyleListBox control calculates text size of the label and fills the rest of the item 
  with the style's graphical representation
- Bugfix: ToolCache.NotifyStyleChanged will no longer be called when selecting a style for a shape 
  instance in property editor
- Bugfix: Diagram's background grid 'wanders' when zooming 
- Bugfix: LineStyles with LineWidth values > 9 will be rendered a little thinner each time the line 
  style selection combobox is opened in property editor
- Bugfix: Diagram.Export() does no longer throw a NullReferenceException in case the diagram has no 
  DisplayService
- Bugfix: Saving a diagram as TIFF image will no longer throw an InvalidArgumentException
- Bugfix: AutoUpgradeEnabled of Save-/LoadFileDialog's will only be set under VISTA (and newer OSes)
- New defines for debug info: DEBUG_DIAGNOSTICS and DEBUG_UI
- Improvement: An exception with proper error message is thrown when trying to save changes in a ChachedRepository 
  without store
- Category of the following properties:
	  CaptionedShapeBase.Text is now "Data" instead of "Text"
	  CaptionedShapeBase.CharacterStyle is now "Appearance" instead of "Text"
	  CaptionedShapeBase.ParagraphStyle is now "Appearance" instead of "Text"
	  Diagram.Name is now "General" instead of "Identification"
	  Diagram.Title is now "General" instead of "Identification"
	  Diagram.Id is now "General" instead of "Identification"
- Property CaptionedShapeBase.CaptionCount is not visible in PropertyGrid any longer
- Bugfix: Orientation of Captions's text is corrected only for shape angle >90� and <270� instead 
  of >90� and <=270�
- Changed value of LayerIds.All from int.MinValue to int.MaxValue.
- Bugfix: Display.CreateImage() no longer throws a NullReferenceException if no DisplayService is assigned
  (means: If Diagram is not displayed in a Display
- New Behavior on Display.ZoomWithMouseWheel = False:
  You can scroll up/down via mouse wheel. When Shift is pressed, you can scroll horizontally and with 
  pressed Ctrl key, you can zoom in/out.
- NShape Designer: Title of the diagram will be displayed properly on the tab when renameing a diagram
- Display: Scrolling with arrow keys enabled
- Display: Improved scroll bar showing/hiding
- New behavior of the PointerTool: When hovering a shape's caption, you can drag the shape away. When 
  clicking without moving the mouse, you can edit the caption.
  To reflect this change, a move cursor is shown instead of the 'Edit Text' cursor.
- New Property "PropertyDisplayMode" of PropertyController: Specifies the behavior of shape properties
  the user may not edit due to NShape security restrictions. 
  Default behavior: Non-editable properties are read only. 
  Alternative behavior: Non-editable properties are invisible.
- Changed ReqiredPermissions attributes of the following properties:
	  Shape.ShapeType:  				None		-> 	ModifyData
	  Style.Name					ModifyData	-> 	Design
	  Style.Title					ModifyData	-> 	Design
	  CapStyle.CapShape				Present		-> 	Design
	  CapStyle.CapSize				Present		-> 	Design
	  CapStyle.ColorStyle				Present		-> 	Design
	  CharacterStyle.ColorStyle			Present		-> 	Design
	  CharacterStyle.FontFamily			Present		-> 	Design
	  CharacterStyle.FontName			Present		-> 	Design
	  CharacterStyle.Size				Present		-> 	Design  
	  CharacterStyle.SizeInPoints			Present		-> 	Design
	  CharacterStyle.Style				Present		-> 	Design
	  CharacterStyle.				Present		-> 	Design
	  ColorStyle.Color				Present		-> 	Design
	  ColorStyle.ConvertToGray			Present		-> 	Design
	  ColorStyle.Transparency			Present		-> 	Design
	  FillStyle.AdditionalColorStyle		Present		-> 	Design
	  FillStyle.BaseColorStyle			Present		-> 	Design
	  FillStyle.ConvertToGrayScale			Present		-> 	Design
	  FillStyle.FillMode				Present		-> 	Design
	  FillStyle.FillPattern				Present		-> 	Design
	  FillStyle.Image				Present		-> 	Design
	  FillStyle.ImageCompressionQuality		Present		-> 	Design
	  FillStyle.ImageGammaCorrection		Present		-> 	Design
	  FillStyle.ImageLayout				Present		-> 	Design
	  FillStyle.ImageTransparency			Present		-> 	Design
	  LineStyle.ColorStyle				Present		-> 	Design
	  LineStyle.DashCap				Present		-> 	Design
	  LineStyle.DashType				Present		-> 	Design
	  LineStyle.LineJoin				Present		-> 	Design
	  LineStyle.LineWidth				Present		-> 	Design
	  ParagraphStyle.Alignment			Present		-> 	Design
	  ParagraphStyle.Trimming			Present		-> 	Design
	  ParagraphStyle.Padding			Present		-> 	Design
	  ParagraphStyle.WordWrap			Present		-> 	Design
- Bugfix: Diagram now draws correctly if the Display component's Left property is > 0.
- Bugfix: Displays created dynamically via code did not invalidate the grips correctly.
- Bugfix: Lines with thick line styles were not inserted correctly in the diagram's spatial index and
  therefore not clickable sometimes
- Switched position of the CustomizableMetaFile's connection- and resize control point.
- Content of Shape.Tag is no longer overwritten when layouting shapes.
- Changed icons for "Visible/Invisible" and "Active/Inactive" in layer list view
- Display.CurrentTool can be set even if no diagram is displayed.
- Bugfix: Design editor control did not redraw correctly when resizing the design editor dialog.
- Preview images for tool icons of text- and label shapes draw their text bigger
- Bugfix: When switching diagrams in NShape designer, selected shapes of the diagram switched to 
  are preselected in the property presenter.
- Shapes can not be dragged outside the display bounds any longer.
- Splitted source code file "LinearShape.cs" into "LinearShape.cs", "PolyLineBase.cs" and "CircularArcBase.cs"
- New Behavior: Shapes with editable captions show a "edit text area" hint.
  This is because the methods CaptionedShapeBase.GetCaptionBounds and CaptionedShapeBase.GetCaptionTextBounds 
  always return placeholder bounds in case there is no text in the caption
- Bugfix: When dragging a connection point to the center point of a shape, the connection points were not 
  highlighted correctly.
- Bugfix: Shapes derived from "TextBase" now return false when moving a control point with AutoSize 
  enabled.
- EntityPropertyDefinitions are sorted by type (EntityFieldDefinitions first InnerObjectDefinitions last)
  in the constructor of the ShapeType. When adding new properties to the persistance interface,
  you do not have to sort the propertyDefinitions manually any longer
- When adding a style to the design, the style's referenced styles are validated (style must exist and 
  style must not be empty).
- NShape designer: Property presenter's model object tab is hidden if there are no model objects in the 
  project
- Display: New context menu item "Copy as Image" that exports the currently selected shapes (or the 
  diagram if no shapes are selected) to the clipboard as PNG and EMF+ file
- NShape designer: New menu items in "Edit" menu: "Copy as Image", "Select All", "Select all Shapes of Type",
  "Select all Shapes of Template" and "Unselect All"
- Bugfix: Corrected/Improved the calculation of the X axis aligned bounding rectangle of the following 
  shapes:
	  ElectricalShapes Transformer
	  ElectricalShapes AutoSwitch
	  ElectricalShapes Rectifier
	  ElectricalShapes Earther
	  FlowChartShapes Terminator
	  FlowChartShapes Input / Output
	  FlowChartShapes Document
	  FlowChartShapes Offpage Connector
	  FlowChartShapes Online Storage
	  FlowChartShapes Drum Storage
	  FlowChartShapes Disk Storage
	  FlowChartShapes Tape Storage
	  FlowChartShapes Preparation
	  FlowChartShapes Manual Input
	  FlowChartShapes Display
	  FlowChartShapes Tape
	  FlowChartShapes Manual Operation
	  FlowChartShapes Card
- Improved ElectricalShapes.Transformer's visual appearance and resize behavior
- Improved ElectricalShapes.Transformer's hit testing
- New Behavior of Display: When zooming out/in, the grid scales with the zoom factor  
- Layouter creates undo commands so the layouting is completly undoable
- Aggregated shapes keep their original ZOrder when aggregating. 
- When creating an aggregated shape with the display's context menu, the aggregation's base shape is 
  no longer the clicked shape but the bottommost shape.
- New demo program: The WPF Demo demonstrates how to integrate NShape in WPF applications with the 
  WindowsFormsHost component. 
- Replaced several string comparisons using the == operator by a string.Compare() using the comparison 
  parameter "InvariantCultureIgnoreCase"
- Marked "CalculateConnectionFoot(int x1, int y1, int x2, int y2)" as [Obsolete] because in the next 
  version it will be replaced
- Bugfix: Diagram's spatial index did store a cell for a shape if a cell with the same cell key already 
  existed
- NShapeExceptions are serializable now
- If the permission for executing a command is not granted, a NShapeSecurityException is thrown instead 
  of an InvalidOperationException.
- The ImageEditor dialog (that appears then selecting an Image for a diagram, shape or style) now sets
  the name of the image file.
- New Behavior of Display: If a shape is moded outside the diagram bounds, the display it will not 
  scroll it into view.
- Bugfix: Solved several issues concerning scrolling and scroll bars of the display component.
- Interface IDisplayPresenter: New Method Update() forces the display to update immediately. 
  This results in a subjectivly faster preview when dragging many shapes, comlpex shapes or picture 
  shapes with large pictures.
- NShape Designer: New projects or projects saved unter a different file name now appear in the 
  "Recent Projects" list right after saving.
- When cancelling or closing the file save dialog while saving, the whole save operation is cancelled 
  and the project will not be closed.
- Bugfix: Padding of TextShape's Paragraph style was calculated twice. Therefore the spacing between 
  shape's outline and text was too large.
- Changed behavior of RepulsionLayouter: The user/the application HAS to select the shapes to be 
  layouted. Labels (which change their relative position to the connected partner shape when moved) 
  are no longer automatically excluded from the layout process. Labels connected to lines via 
  PointToShape connection no longer cause a crash.
- Changes in Tutorial:
  * Shapes are now created in the middle of the diagram as they are not visible without scrolling 
    otherwise
  * Added necessary call to Layouter.Prepare() before executing the layouter
  * Changed references to NShape assemblies to "Version Specific" = False
- Demo program "Shuffle Demo" renamed to "VB Demo"
- Bugfix: SQLStore now stores inner objects "ColumnNames" of SoftwareArchitectureShapes.Entity shapes.
- Bugfix: When redoing an undone create layers command, the re-created layers do not show in layer editor
- Bugfix: AggregatedShape threw Exception when setting Width or Height to 0
- Bugfix: FlowLayouter no longer crashes when layouting in "Bottom Up" mode.
- Bugfix: NShape Designer calls SaveAs when trying to save a new project but database repositories do 
  not support "SaveAs".
- Changed behavior: When creating a new template from a shape (via context menu), the shape of the new 
  template is not dependent from any other template.
- When saving a project, ...
  * ... the project is saved in a temporary file ("~ProjectName.xml") that is renamed after saving 
    completed successfully
  * ... when overwriting or saving an existing project, a backup file is created ("ProjectName.bak")
- Bugfix: Changing a template's shape only worked once.
- Workaround: On 64 bit OSes, Metafile.PlayRecord() does not play certain records. Unfortunately, one 
  of the affected records is "CreateBrushIndirect which is used for replacing colors in the 
  CustomizableMetafile class. As Microsoft has not corrected the bug yet (nor shown a satisfying 
  workaround for it), we've changed the CustomizableMetafile.


====================================================================================================
Changes in 1.0.6:
====================================================================================================

- Replaced all EventHandler types with generic EventHandler<TEventArgs>:
	TemplateEditorSelectedEventHandler 	=>  	EventHandler<TemplateEditorEventArgs>
	ToolEventHandler			=>	EventHandler<ToolEventArgs>
	StyleSelectedEventHandler		=> 	EventHandler
- EventArgs ShowTemplateEditorEventArgs renamed to TemplateEditorEventArgs
- Event IDiagramPresenter.ShapeInsert renamed to IDiagramPresenter.ShapesInserted
- Event IDiagramPresenter.ShapeRemove renamed to IDiagramPresenter.ShapesRemoved
- DiagramPresenterShapeEventArgs renamed to DiagramPresenterShapesEventArgs

- Event IDiagramPresenter.ShapesInserted will now be raised for the display that executed the tool inserting the shape
- Event IDiagramPresenter.ShapesRemoved will now be raised for the display that executed the tool inserting the shape
- Bugfix: ModelObjects in custom library namespaces were not found when saving to XML
- XML Summaries added for all public and protected Interfaces, Classes, Methods and Properties (No more Warnings)
- Errors in ScrollBar position calculation when displaying a new diagram fixed
- ScrollBars update now when changing diagram's Width/Height properties in PropertyPresenter
- LayerListView entering rename mode on every click fixed
- Icons of LayerListview improved (more contrast for better visibility on high resolutions
- DiagramPresenter/DiagramController: Methods added for adding/assigning/removing shapes to/from layers
- Shape's "Layer" property shows as readonly property in PropertyPresenter
- New Event IDiagramPresenter.UserMessage used for notifying the user that a non-visual component cannot execute a command/action or encountered a problem/exceptional situation
- "Label"-Shape: Improved "Pin" icon when not connected (bigger and looks more like pin)
- "Label"-Shape: Line between shape and gluepoint is drawn when not connected
- New "ModelDemo" demonstarting the usage of IModelObject and visual property binding between shapes and model objects.
- Added a descriptive text for most example diagrams
- Display: Fixed issue that hit test on top border will always fail
- Click on diagram shows diagram properties in PropertyPresenter
- TestSuite: Added new ModelMapping test
- New Property IModelObject.ShapeCount
- StyleListBox / UITypeEditor for Styles: New Item "More..." that opens the desgign editor on a click
- Added Constructor overloads for Style UITypeEditor: New property Project (needed for item "More...")
- StyleListBox / UITypeEditor for Styles: Styles are sorted by their titles now
- EntityShape: Fixed endless loop / infinite memory allocation when editing the "ColumnNames" property in PropertyPresenter
- EntityShape: Lines connected to GluePoints of additional labels will be disconnected when deleting the labels
- Visual mapping implementations added for some shape properties that were missing
- InPlace-Texteditor for Shapes: Fixed invalidation issues
- InPlace-Texteditor for Shapes: Grows/shrinks correctly when editing, especially for shapes with AutoSize-Property depending on the shape's text (Text and aLabel shapes)
- PictureShape: New base class PictureBase in NShape core assembly
- PictureShape: Text will be displayed below the image, image will be displayed in the remaining area
- Tool: Added minimum drag distance: the mouse has to move the Minimum drag distance defined by the OS before a mouse move with pressed button is considered as drag action
- Tool: Improved internal event handling
- Line Tools: New feature "Extending lines" added
- NShape Designer: Added EventMonitor tool
- NShape designer: Improved handling of "New Project..." and "Save project" / "Save Project as..."
- NShape designer: Added new "Debug Toolbar", only available in debug mode for visualizing invalidated areas and occupation of the diagram's spatial index
- NShape designer: Added new setting "Grid Color" to display settings dialog.
- NShape designer: Diagram tabs's text will change accordingly when modifying the diagram's title
- NShape Designer: Snap-To-Grid can emporarily deactivated by holding Ctrl key
- Fixed issue that linearShapes cannot connect to ConnectionPoints of shapes
- When deleting a selected (point-to-shape connected) linear shape, the highlighted connected shapes will be invalidated correctly now
- Missing "CommandExecuted" event (before "CommandAdded" event) is now raised when calling Project.ExecuteCommand()
- "Export Diagram" dialog: Fixed "OutOfMemory" Exception when selecting high DPI values for large diagrams (resulting in bitmaps larger than 16000 x 16000)
- "Export Diagram" dialog: Added file format to the description (file format selection radio buttons)
- Changed default background color of diagrams to white (in fact a "White to very light gray" gradient)
- Changed parameter order in some of the Geometry functions so that all methods follow the same parameter ordering scheme and match the method name: E.g. RectangleContainsPoint(p, rectangle) -> RectangleContainsPoint(rectangle, p)
- Added descriptions for styles and a "what to edit where" overview table to documentation
- Dialogs of the WinFormsUI assembly now display the icon of their host application instead of the NShape icon
- Reordered Tab-sequence of controls in WinformsUI's dialogs
- Removing a shape from LayerIds.All / LayerIds.None will work now as expected (shape will be removed from all/no layers)
- DesignEditor: Fixed bug when changing a style's name, undoing and redoing that change
- Unified version info for all assemblies
- New Events: IRepository.ShapeConnectionInserted and IRepository.ShapeConnectionDeleted
- New Methods: IDiagramPresenter.InsertShapes
- New Methods: IDiagramPresenter.InsertShape(Shape shape), IDiagramPresenter.InsertShapes(IEnumerable<Shape> shapes), IDiagramPresenter.DeleteShape(Shape shape, bool withModelObjects), IDiagramPresenter.DeleteShapes(IEnumerable<Shape> shapes, bool withModelObjects), IDiagramPresenter.Cut(), IDiagramPresenter.Copy(), IDiagramPresenter.Paste()
- Display: Property "ShowScrollbars" works correct now
- RepulsionLayouter: Fixed undesired behaviour with unconnected lines and connected Label shapes
- Fixed some issues when splitting aggregated shapes created from a template
- "Find model" in ModelTreeViewPresenter automatically scrolls to found shapes in die current diagram
- Images of XML repositories are stored using the image's name and its id instead of using its id only: "[ImageName] [Id].png" instead of "[Id].png"


Version 1.0.5
Version 1.0.4
Version 1.0.3
Version 1.0.2
Version 1.0.1
Version 1.0.0

Initial Release


===============================================================================
Your dataWeb team