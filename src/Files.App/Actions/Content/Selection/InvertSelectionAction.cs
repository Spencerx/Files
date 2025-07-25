﻿// Copyright (c) Files Community
// Licensed under the MIT License.

namespace Files.App.Actions
{
	internal sealed class InvertSelectionAction : IAction
	{
		private readonly IContentPageContext context;

		public string Label
			=> Strings.InvertSelection.GetLocalizedResource();

		public string Description
			=> Strings.InvertSelectionDescription.GetLocalizedResource();

		public RichGlyph Glyph
			=> new(themedIconStyle: "App.ThemedIcons.SelectInvert");

		public bool IsExecutable
		{
			get
			{
				if (context.PageType is ContentPageTypes.Home)
					return false;

				if (!context.HasItem)
					return false;

				var page = context.ShellPage;
				if (page is null)
					return false;

				bool isRenaming = page.SlimContentPage.IsRenamingItem;
				return !isRenaming;
			}
		}

		public InvertSelectionAction()
		{
			context = Ioc.Default.GetRequiredService<IContentPageContext>();
		}

		public Task ExecuteAsync(object? parameter = null)
		{
			context?.ShellPage?.SlimContentPage?.ItemManipulationModel?.InvertSelection();

			return Task.CompletedTask;
		}
	}
}
