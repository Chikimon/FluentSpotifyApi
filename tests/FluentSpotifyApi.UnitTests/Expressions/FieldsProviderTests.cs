﻿using System;
using FluentAssertions;
using FluentSpotifyApi.Expressions.Fields;
using FluentSpotifyApi.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FluentSpotifyApi.UnitTests.Expressions
{
    [TestClass]
    public class FieldsProviderTests
    {
        [TestMethod]
        public void ShouldGetFields()
        {
            // Arrange + Act
            var result = FieldsProvider.Get<FullPlaylist>(builder => builder
                .Exclude(playlist => playlist.Tracks.Items[0].Track.Album.Name)
                .Exclude(playlist => playlist.Tracks.Items[0].Track.Album.Images)
                .Exclude(playlist => playlist.Tracks.Items[0].Track.Artists[0].Id)
                .Include(playlist => playlist.Name));

            // Assert
            result.Should().Be("(tracks(items(track(album(!name,images),artists(!id)))),name)");
        }

        [TestMethod]
        public void ShouldGetNullWhenThereIsNoExpression()
        {
            // Arrange + Act + Assert
            FieldsProvider.Get<FullPlaylist>(builder => { }).Should().BeNull();
        }

        [TestMethod]
        public void ShouldReplaceExcludeWithInclude()
        {
            // Arrange + Act
            var result = FieldsProvider.Get<FullPlaylist>(builder => builder
                .Exclude(playlist => playlist.Images[0].Height)
                .Exclude(playlist => playlist.Tracks.Items[0].Track.Popularity)
                .Exclude(playlist => playlist.Tracks.Items[0].Track.IsPlayable)                
                .Include(playlist => playlist.Tracks.Items[0].Track.Name));

            // Assert
            result.Should().Be("(images(!height),tracks(items(track(name))))");
        }

        [TestMethod]
        public void ShouldReplaceIncludeWithExclude()
        {
            // Arrange + Act
            var result = FieldsProvider.Get<FullPlaylist>(builder => builder
                .Include(playlist => playlist.Images[0].Height)
                .Include(playlist => playlist.Tracks.Items[0].Track.Popularity)
                .Include(playlist => playlist.Tracks.Items[0].Track.Name)
                .Exclude(playlist => playlist.Tracks.Items[0].Track));

            // Assert
            result.Should().Be("(images(height),tracks(items(!track)))");
        }

        [TestMethod]
        public void ShouldThrowArgumentExceptionWhenExpressionIsInvalid()
        {
            // Arrange
            Action action = () => FieldsProvider.Get<FullPlaylist>(builder => builder.Include<object>(playlist => null));

            // Act + Assert
            action.ShouldThrow<ArgumentException>().Which.Message.Should().Be("The expression must be a member expression referencing the input parameter.");
        }
    }
}