using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Editor;
using Sandbox;
using Sandbox.Diagnostics;
using FileSystem = Editor.FileSystem;

namespace Braxnet;

[EditorApp( "FileRenamer", "drive_file_rename_outline", "File Renamer" )]
public class FileRenamer : Window
{
	private static Logger Log = new("FileRenamer");

	[ConVar( "frn_test" )]
	public static bool TestVarl { get; set; }

	public FileRenamer()
	{
		WindowTitle = "Hello";
		MinimumSize = new Vector2( 300, 500 );

		var canvas = new Widget( null );
		canvas.Layout = Layout.Column();
		canvas.Layout.Spacing = 5;
		canvas.Layout.Margin = 5;

		var introText = new Label();
		introText.Text =
			"First rename your component in your IDE, then use this tool to fix all dependants.\nMake sure to back up or use version control before using this tool.";
		canvas.Layout.Add( introText );

		var inputComponentName = new LineEdit();
		inputComponentName.PlaceholderText = "MyOldNamespace.SourceComponent";
		canvas.Layout.Add( inputComponentName );

		/*var inputComponentNameValidator = new Label();
		inputComponentNameValidator.Text = "Type the name of the component you want to rename";
		canvas.Layout.Add( inputComponentNameValidator );

		inputComponentName.TextChanged += ( string text ) =>
		{
			var component = TypeLibrary.GetType( text );
			if ( component == null )
			{
				inputComponentNameValidator.Text = "Component not found";
			}
			else
			{
				inputComponentNameValidator.Text = $"Component found: {component.Namespace}.{component.Name}";
			}
		};*/

		var outputComponentName = new LineEdit();
		outputComponentName.PlaceholderText = "MyNewNamespace.DestinationComponent";
		canvas.Layout.Add( outputComponentName );

		var button = new Button( "Rename" );
		button.Pressed += () => RenameComponent( inputComponentName.Text, outputComponentName.Text );
		canvas.Layout.Add( button );

		Canvas = canvas;
	}

	private void RenameComponent( string inputComponentName, string outputComponentName )
	{
		/*var inputComponent = TypeLibrary.GetType( inputComponentName );
		if ( inputComponent == null )
		{
			Log.Error( $"Component not found: {inputComponentName}" );
			return;
		}

		var outputComponent = TypeLibrary.GetType( outputComponentName );
		if ( outputComponent != null )
		{
			Log.Error( $"Component already exists: {outputComponentName}" );
			return;
		}*/

		/*var outputNamespace = outputComponentName.Substring( 0, outputComponentName.LastIndexOf( '.' ) );
		var outputClassName = outputComponentName.Substring( outputComponentName.LastIndexOf( '.' ) + 1 );

		var inputNamespace = inputComponent.Namespace;
		var inputClassName = inputComponent.Name;

		var codeRoot = Project.Current.GetCodePath();*/

		var rootAssetPath = FileSystem.Mounted.GetFullPath( "." );
		var fileTypesToSearch = new string[] { ".scene", ".prefab" };

		var files = System.IO.Directory.GetFiles( rootAssetPath, "*.*", System.IO.SearchOption.AllDirectories )
			.Where( x => fileTypesToSearch.Contains( System.IO.Path.GetExtension( x ) ) );

		foreach ( var file in files )
		{
			var text = System.IO.File.ReadAllText( file );

			if ( text.Contains( inputComponentName ) )
			{
				// text = text.Replace( inputComponentName, outputComponentName );
				text = text.Replace( $"\"__type\": \"{inputComponentName}\"",
					$"\"__type\": \"{outputComponentName}\"" );
				System.IO.File.WriteAllText( file, text );
				Log.Info( $"Replaced {inputComponentName} with {outputComponentName} in {file}" );
			}
		}

		Log.Info( $"Renamed {inputComponentName} to {outputComponentName}" );
	}

	[Event( "asset.contextmenu", Priority = 50 )]
	public static void RenameFileAssetContext( AssetContextMenu e )
	{
		// Are all the files we have selected sound assets?
		// if ( !e.SelectedList.All( x => x.AssetType == AssetType.SoundFile ) )
		// 	return;

		e.Menu.AddOption( $"Rename/move and fix dependants", "audio_file",
			action: () => ContextMenuClick( e.SelectedList ) );
	}

	private static string lastDirectory = "";

	private static void ContextMenuClick( List<AssetEntry> assetEntries )
	{
		/*if ( assetEntries.Count > 1 )
		{
			throw new System.Exception( "Only one asset can be renamed at a time" );
		}

		var asset = assetEntries.First();*/

		lastDirectory = "";

		if ( assetEntries.Count > 1 )
		{
			_ = RenameMultipleAssetsPopup( assetEntries );
		}
		else
		{
			RenameSingleAssetPopup( assetEntries.First() );
		}
	}

	private static void RenameSingleAssetPopup( AssetEntry asset )
	{
		/*if ( asset.AssetType == AssetType.Model )
		{
			throw new System.Exception( "Cannot rename models due to not all dependants being added" );
		}*/

		if ( string.IsNullOrEmpty( lastDirectory ) )
		{
			lastDirectory = System.IO.Path.GetDirectoryName( asset.AbsolutePath );
		}

		var originalPath = asset.AbsolutePath;

		var fd = new FileDialog( null );
		fd.Title = "Rename asset..";
		// fd.Directory = System.IO.Path.GetDirectoryName( originalPath );
		fd.Directory = lastDirectory;
		fd.DefaultSuffix = System.IO.Path.GetExtension( originalPath );
		fd.SelectFile( System.IO.Path.GetFileName( originalPath ) );

		fd.SetModeSave();
		fd.SetNameFilter( "All Files (*.*)" );

		if ( !fd.Execute() )
			return;

		var newPath = fd.SelectedFile;

		_ = RenameSingleAsset( asset, newPath );
	}

	private static async Task RenameMultipleAssetsPopup( List<AssetEntry> assetEntries )
	{
		var fd = new FileDialog( null );
		fd.Title = "Move assets..";
		fd.Directory = assetEntries.First().AbsolutePath;
		// fd.SetModeSave();
		fd.SetFindDirectory();

		if ( !fd.Execute() )
			return;

		var newPath = fd.SelectedFile;

		using var progress = Progress.Start( "Renaming assets" );
		var token = Progress.GetCancel();

		foreach ( var asset in assetEntries )
		{
			var originalPath = asset.AbsolutePath;

			var relativePath =
				System.IO.Path.GetRelativePath( System.IO.Path.GetDirectoryName( originalPath ), newPath );
			var newAssetPath = System.IO.Path.Combine( newPath, System.IO.Path.GetFileName( originalPath ) );

			Progress.Update( $"Renaming {originalPath} to {newAssetPath}", assetEntries.IndexOf( asset ) + 1,
				assetEntries.Count );

			await RenameSingleAsset( asset, newAssetPath );

			if ( token.IsCancellationRequested )
				return;
		}
	}

	private static async Task RenameSingleAsset( AssetEntry asset, string newPath )
	{
		var originalPath = asset.AbsolutePath;

		if ( newPath == originalPath )
		{
			Log.Error( "New path is the same as the original path" );
			return;
		}

		if ( System.IO.File.Exists( newPath ) )
		{
			// throw new System.Exception( $"File already exists: {newPath}" );
			Log.Error( $"File already exists: {newPath}" );
			return;
		}

		Log.Info( $"!!! Renaming {originalPath} to {newPath} !!!" );

		// System.IO.File.Move( originalPath, newPath );

		var dependants = await GetDependants( asset );

		foreach ( var dependant in dependants.Item1 )
		{
			Log.Info( $"Dependant: {dependant}" );
			await FixDependant( asset, dependant, originalPath, newPath );
		}

		foreach ( var dependantPath in dependants.Item2 )
		{
			Log.Info( $"Dependant path found: {dependantPath}" );
		}

		Log.Info( $"!!! Renamed {originalPath} to {newPath} !!!" );

		CleanupCompiledAsset( asset, originalPath );

		// System.IO.File.Move( originalPath, newPath );
		// asset.Rename(  );

		asset.FileInfo.MoveTo( newPath );
		AssetSystem.RegisterFile( newPath );
	}

	private static string GetAssetRelativePathFromFullPath( string fullPath )
	{
		var basePath = FileSystem.Mounted.GetFullPath( "." );
		return System.IO.Path.GetRelativePath( basePath, fullPath ).Replace( '\\', '/' );
	}

	private static readonly string[] ModelSourceExtensions = new string[] { ".fbx", ".obj", ".dmx" };
	private static readonly string[] TextureSourceExtensions = new string[] { ".png", ".jpg", ".jpeg", ".tga" };
	private static readonly string[] SoundSourceExtensions = new string[] { ".wav", ".mp3", ".ogg", ".flac" };

	private static async Task<(List<Asset>, List<string>)> GetDependants( AssetEntry asset )
	{
		var dependants = new List<Asset>();
		var dependantPaths = new List<string>();

		// get built in
		dependants.AddRange( asset.Asset.GetDependants( false ) );

		// fbx files are not included in the built in dependants for models
		if ( ModelSourceExtensions.Contains( System.IO.Path.GetExtension( asset.AbsolutePath ) ) )
		{
			// look through all vmdl files in the project
			var basePath = FileSystem.Mounted.GetFullPath( "." );
			var vmdlFiles = System.IO.Directory.GetFiles( basePath, "*.vmdl", System.IO.SearchOption.AllDirectories );

			foreach ( var vmdlFile in vmdlFiles )
			{
				var text = await System.IO.File.ReadAllTextAsync( vmdlFile );
				if ( text.Contains( GetAssetRelativePathFromFullPath( asset.AbsolutePath ) ) )
				{
					var vmdlAsset = AssetSystem.FindByPath( vmdlFile );
					if ( vmdlAsset != null )
					{
						dependants.Add( vmdlAsset );
					}
					else
					{
						Log.Error( $"Dependant vmdl not found: {vmdlFile}" );
					}

					dependantPaths.Add( vmdlFile );
					Log.Info( $"Found dependant {vmdlFile} for {asset.AbsolutePath} fbx" );
				}
			}
		}

		// image files are not included in the built in dependants for materials
		if ( TextureSourceExtensions.Contains( System.IO.Path.GetExtension( asset.AbsolutePath ) ) )
		{
			// look through all vmat files in the project
			var basePath = FileSystem.Mounted.GetFullPath( "." );
			var vmatFiles = System.IO.Directory.GetFiles( basePath, "*.vmat", System.IO.SearchOption.AllDirectories );

			foreach ( var vmatFile in vmatFiles )
			{
				var text = System.IO.File.ReadAllText( vmatFile );
				if ( text.Contains( GetAssetRelativePathFromFullPath( asset.AbsolutePath ) ) )
				{
					var vmatAsset = AssetSystem.FindByPath( vmatFile );
					if ( vmatAsset != null )
					{
						dependants.Add( vmatAsset );
					}
					else
					{
						Log.Error( $"Dependant vmat not found: {vmatFile}" );
					}

					dependantPaths.Add( vmatFile );
					Log.Info( $"Found dependant {vmatFile} for {asset.AbsolutePath} image" );
				}
			}
		}

		// sound files have their extensions changed to .vsnd when referenced by sound events
		if ( asset.AssetType == AssetType.SoundFile )
		{
		}

		return (dependants, dependantPaths);
	}

	private static void CleanupCompiledAsset( AssetEntry asset, string originalPath )
	{
		var compiledPath = asset.Asset.GetCompiledFile( true );

		if ( string.IsNullOrEmpty( compiledPath ) )
		{
			Log.Error( $"Compiled asset not found for {originalPath}" );
			return;
		}

		if ( !System.IO.File.Exists( compiledPath ) )
		{
			Log.Error( $"Compiled asset not found: {compiledPath}" );
			return;
		}

		Log.Info( $"Cleaning up compiled asset {compiledPath}" );

		System.IO.File.Delete( compiledPath );
	}

	private static async Task FixDependant( AssetEntry asset, Asset dependant, string originalPath, string newPath )
	{
		if ( asset == null )
		{
			Log.Error( "Asset is null" );
			return;
		}

		if ( dependant == null )
		{
			Log.Error( "Dependant is null" );
			return;
		}

		var dependantPath = dependant.AbsolutePath;

		if ( dependant.AssetType == AssetType.MapFile )
		{
			await FixMapFile( asset, dependant, originalPath, newPath );
			return;
		}

		if ( !System.IO.File.Exists( dependantPath ) )
		{
			Log.Error( $"Dependant not found: {dependantPath}" );
			return;
		}

		var text = await System.IO.File.ReadAllTextAsync( dependantPath );

		// var relativeOriginalPath =
		// 	System.IO.Path.GetRelativePath( FileSystem.Mounted.GetFullPath( "." ), originalPath ).Replace( '\\', '/' );
		// var relativeNewPath = System.IO.Path.GetRelativePath( FileSystem.Mounted.GetFullPath( "." ), newPath )
		// 	.Replace( '\\', '/' );
		var relativeOriginalPath = GetAssetRelativePathFromFullPath( originalPath );
		var relativeNewPath = GetAssetRelativePathFromFullPath( newPath );

		Log.Info( $"Replacing {relativeOriginalPath} with {relativeNewPath} in {dependantPath}" );

		// replace sound file extensions with .vsnd when referenced by sound events
		if ( asset.AssetType == AssetType.SoundFile )
		{
			relativeOriginalPath = System.IO.Path.ChangeExtension( relativeOriginalPath, ".vsnd" );
			relativeNewPath = System.IO.Path.ChangeExtension( relativeNewPath, ".vsnd" );
		}

		text = text.Replace( relativeOriginalPath, relativeNewPath );

		await System.IO.File.WriteAllTextAsync( dependantPath, text );

		dependant.Compile( true );
		asset.Asset.Compile( true );

		Log.Info( $"Fixed dependant {dependantPath}" );
	}

	// vmap files are stored in binary, so they need to be converted to text before replacing paths
	private static async Task FixMapFile( AssetEntry asset, Asset dependant, string originalPath, string newPath )
	{
		var engineRoot = FileSystem.Root.GetFullPath( "." );

		var dmxConvertPath = System.IO.Path.Combine( engineRoot, "bin", "win64", "dmxconvert.exe" );

		if ( !System.IO.File.Exists( dmxConvertPath ) )
		{
			Log.Error( $"dmxconvert.exe not found at {dmxConvertPath}" );
			return;
		}

		var mapFilePath = dependant.AbsolutePath;

		var tempPath = System.IO.Path.GetTempFileName();

		var args = $"-i \"{mapFilePath}\" -ie vmap -o \"{tempPath}\" -oe keyvalues2";

		var process = new System.Diagnostics.Process();
		process.StartInfo.FileName = dmxConvertPath;
		process.StartInfo.Arguments = args;
		process.StartInfo.UseShellExecute = false;
		process.StartInfo.RedirectStandardOutput = true;
		process.StartInfo.RedirectStandardError = true;
		process.Start();
		await process.WaitForExitAsync();

		// Log.Info( $"{dmxConvertPath} {args}" );
		// Log.Info( process.StandardOutput.ReadToEnd() );
		// Log.Info( process.StandardError.ReadToEnd() );

		if ( process.ExitCode != 0 )
		{
			Log.Error( $"dmxconvert.exe vmap->text failed with exit code {process.ExitCode}" );
			return;
		}

		var text = await System.IO.File.ReadAllTextAsync( tempPath );

		var relativeOriginalPath = GetAssetRelativePathFromFullPath( originalPath );
		var relativeNewPath = GetAssetRelativePathFromFullPath( newPath );

		Log.Info( $"Replacing {relativeOriginalPath} with {relativeNewPath} in {mapFilePath}" );

		text = text.Replace( relativeOriginalPath, relativeNewPath );

		Log.Info( $"Writing fixed text to {tempPath}" );

		await System.IO.File.WriteAllTextAsync( tempPath, text );

		Log.Info( $"Converting fixed text back to vmap: {mapFilePath}" );

		args = $"-i \"{tempPath}\" -ie keyvalues2 -o \"{mapFilePath}\" -of vmap";

		process = new System.Diagnostics.Process();
		process.StartInfo.FileName = dmxConvertPath;
		process.StartInfo.Arguments = args;
		process.StartInfo.UseShellExecute = false;
		process.StartInfo.RedirectStandardOutput = true;
		process.StartInfo.RedirectStandardError = true;
		process.Start();

		await process.WaitForExitAsync();

		// Log.Info( $"{dmxConvertPath} {args}" );
		// Log.Info( process.StandardOutput.ReadToEnd() );
		// Log.Info( process.StandardError.ReadToEnd() );

		if ( process.ExitCode != 0 )
		{
			Log.Error( $"dmxconvert.exe text->vmap failed with exit code {process.ExitCode}" );
			return;
		}

		// System.IO.File.Delete( tempPath );

		Log.Info( $"Fixed dependant map {mapFilePath}" );
	}
}
