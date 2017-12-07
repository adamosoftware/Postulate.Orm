Postulate.Orm.Core Readme
-------------------------

Thanks for installing Postulate ORM! Next steps:

1. Install the Schema Merge tool:
   https://adamosoftware.blob.core.windows.net/install/PostulateSchemaMergeSetup.exe
   This installer is continually updated as improvements become available.
   This tool allows you to merge model class changes with your physical database via a WinForms UI.
   Currently, Schema Merge works only with SQL Server. A MySQL version is coming.

   Source for the Schema Merge app is here: https://github.com/adamosoftware/Postulate.Orm/tree/master/MergeUI
   Source for Postulate.Orm.Core is here one level up here: https://github.com/adamosoftware/Postulate.Orm

   A walkthrough video is here: https://vimeo.com/219400011
   This video is a little outdated, but it's still good enough for the moment. I'm getting ready to do a new walkthrough video.

   After installing, there are three ways to invoke it:

   - You can create an External Tool in Visual Studio that launches it. Use this macro in the Arguments:
     $(SolutionDir)\$(SolutionFileName)
     The first time you run it, Schema Merge will prompt you for the location of your Models .dll within your solution.
     A small .json file is created in your solution folder to remember your selection.

   - You can create a post-build event on your Models project that calls MergeUI.exe (full path will be in the desktop shortcut) followed by the 
     path to your Models .dll. This will cause Schema Merge to offer to sync with the database after every successful build.

   - You can use the desktop shortcut to launch the Postulate Scheme Merge app standalone. You are prompted for the model class .dll location on startup.

2. Install either of the platform-specific packages:
   Postulate.Orm.SqlServer
   Postulate.Orm.MySql   

   Both packages have full CRUD support, but at this time only the SqlServer version works with Schema Merge.

3. If you're building an ASP.NET MVC app, install package Postulate.Mvc to get a lot of Postulate productivity features for MVC development.
   Source and more info is here: https://github.com/adamosoftware/Postulate.Mvc

Don't hesitate to reach to me if you have questions.

Adam O'Neil
adamosoftware@gmail.com
