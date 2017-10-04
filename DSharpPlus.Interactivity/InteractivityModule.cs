﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace DSharpPlus.Interactivity
{
    #region Extension stuff
    public static class InteractivityExtension
    {
        public static InteractivityModule UseInteractivity(this DiscordClient c, InteractivityConfiguration cfg)
        {
            if (c.GetModule<InteractivityModule>() != null)
                throw new Exception("Interactivity module is already enabled for this client!");

            var m = new InteractivityModule(cfg);
            c.AddModule(m);
            return m;
        }

        public static IReadOnlyDictionary<int, InteractivityModule> UseInteractivity(this DiscordShardedClient c, InteractivityConfiguration cfg)
        {
            var modules = new Dictionary<int, InteractivityModule>();

            c.InitializeShardsAsync().GetAwaiter().GetResult();

            foreach (var shard in c.ShardClients.Select(xkvp => xkvp.Value))
            {
                var m = shard.GetModule<InteractivityModule>();
                if (m == null)
                    m = shard.UseInteractivity(cfg);

                modules.Add(shard.ShardId, m);
            }

            return new ReadOnlyDictionary<int, InteractivityModule>(modules);
        }

        public static InteractivityModule GetInteractivityModule(this DiscordClient c)
        {
            return c.GetModule<InteractivityModule>();
        }

        public static IReadOnlyDictionary<int, InteractivityModule> GetInteractivityModule(this DiscordShardedClient c)
        {
            var modules = new Dictionary<int, InteractivityModule>();

            c.InitializeShardsAsync().GetAwaiter().GetResult();

            foreach (var shard in c.ShardClients.Select(xkvp => xkvp.Value))
                modules.Add(shard.ShardId, shard.GetModule<InteractivityModule>());

            return new ReadOnlyDictionary<int, InteractivityModule>(modules);
        }

        public static IEnumerable<string> Split(this string str, int chunkSize)
        {
            var len = str.Length;
            var i = 0;

            while (i < len)
            {
                var size = Math.Min(len - i, chunkSize);
                yield return str.Substring(i, size);
                i += size;
            }
        }
    }
    #endregion

    public class InteractivityModule : BaseModule
    {
        private InteractivityConfiguration Config { get; }

        internal InteractivityModule(InteractivityConfiguration cfg)
        {
            this.Config = cfg;
        }

        protected internal override void Setup(DiscordClient client)
        {
            this.Client = client;
        }

        #region Message
        public async Task<MessageContext> WaitForMessageAsync(Func<DiscordMessage, bool> predicate, TimeSpan? timeoutoverride = null)
        {
            TimeSpan timeout = Config.Timeout;
            if (timeoutoverride != null)
                timeout = (TimeSpan)timeoutoverride;

            var tsc = new TaskCompletionSource<MessageContext>();
            var ct = new CancellationTokenSource(timeout);
            ct.Token.Register(() => tsc.TrySetResult(null));

            try
            {
                this.Client.MessageCreated += Handler;
                var result = await tsc.Task;
                return result;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                this.Client.MessageCreated -= Handler;
            }

            #region Handler
            async Task Handler(MessageCreateEventArgs e)
            {
                await Task.Yield();
                if (predicate(e.Message))
                {
                    var mc = new MessageContext()
                    {
                        Interactivity = this,
                        Message = e.Message
                    };
                    tsc.TrySetResult(mc);
                    return;
                }
            }
            #endregion
        }
        #endregion

        #region Reaction
        public async Task<ReactionContext> WaitForReactionAsync(Func<DiscordEmoji, bool> predicate, TimeSpan? timeoutoverride = null)
        {
            TimeSpan timeout = Config.Timeout;
            if (timeoutoverride != null)
                timeout = (TimeSpan)timeoutoverride;

            var tsc = new TaskCompletionSource<ReactionContext>();
            var ct = new CancellationTokenSource(timeout);
            ct.Token.Register(() => tsc.TrySetResult(null));

            try
            {
                this.Client.MessageReactionAdded += Handler;

                var result = await tsc.Task;
                return result;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                this.Client.MessageReactionAdded -= Handler;
            }

            #region Handler
            async Task Handler(MessageReactionAddEventArgs e)
            {
                await Task.Yield();
                if (predicate(e.Emoji))
                {
                    var rc = new ReactionContext()
                    {
                        Channel = e.Channel,
                        Emoji = e.Emoji,
                        Message = e.Message,
                        User = e.User,
                        Interactivity = this
                    };
                    tsc.TrySetResult(rc);
                    return;
                }
            }
            #endregion
        }

        public async Task<ReactionContext> WaitForReactionAsync(Func<DiscordEmoji, bool> predicate, DiscordUser user, TimeSpan? timeoutoverride = null)
        {
            TimeSpan timeout = Config.Timeout;
            if (timeoutoverride != null)
                timeout = (TimeSpan)timeoutoverride;

            var user_id = user.Id;
            var tsc = new TaskCompletionSource<ReactionContext>();
            var ct = new CancellationTokenSource(timeout);
            ct.Token.Register(() => tsc.TrySetResult(null));

            try
            {
                this.Client.MessageReactionAdded += Handler;

                var result = await tsc.Task;
                return result;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                this.Client.MessageReactionAdded -= Handler;
            }

            #region Handler
            async Task Handler(MessageReactionAddEventArgs e)
            {
                await Task.Yield();
                if (predicate(e.Emoji))
                {
                    if (e.User.Id == user_id)
                    {
                        var rc = new ReactionContext()
                        {
                            Channel = e.Channel,
                            Emoji = e.Emoji,
                            Message = e.Message,
                            User = e.User,
                            Interactivity = this
                        };
                        tsc.TrySetResult(rc);
                        return;
                    }
                }
            }
            #endregion
        }

        public async Task<ReactionContext> WaitForMessageReactionAsync(Func<DiscordEmoji, bool> predicate, DiscordMessage msg, DiscordUser user = null, TimeSpan? timeoutoverride = null)
        {
            TimeSpan timeout = Config.Timeout;
            if (timeoutoverride != null)
                timeout = (TimeSpan)timeoutoverride;

            var message_id = msg.Id;
            var tsc = new TaskCompletionSource<ReactionContext>();
            var ct = new CancellationTokenSource(timeout);
            ct.Token.Register(() => tsc.TrySetResult(null));

            try
            {
                this.Client.MessageReactionAdded += Handler;

                var result = await tsc.Task;
                return result;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                this.Client.MessageReactionAdded -= Handler;
            }

            #region Handler
            async Task Handler(MessageReactionAddEventArgs e)
            {
                await Task.Yield();
                if (predicate(e.Emoji))
                {
                    if (e.Message.Id == message_id)
                    {
                        if (user == null || e.User.Id == user?.Id)
                        {
                            var rc = new ReactionContext()
                            {
                                Channel = e.Channel,
                                Emoji = e.Emoji,
                                Message = e.Message,
                                User = e.User,
                                Interactivity = this
                            };
                            tsc.TrySetResult(rc);
                            return;
                        }
                    }
                }
            }
            #endregion
        }

        public async Task<ReactionContext> WaitForMessageReactionAsync(DiscordMessage msg, DiscordUser user = null, TimeSpan? timeoutoverride = null)
        {
            TimeSpan timeout = Config.Timeout;
            if (timeoutoverride != null)
                timeout = (TimeSpan)timeoutoverride;

            var message_id = msg.Id;
            var tsc = new TaskCompletionSource<ReactionContext>();
            var ct = new CancellationTokenSource(timeout);
            ct.Token.Register(() => tsc.TrySetResult(null));

            try
            {
                this.Client.MessageReactionAdded += Handler;

                var result = await tsc.Task;
                return result;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                this.Client.MessageReactionAdded -= Handler;
            }

            #region Handler
            async Task Handler(MessageReactionAddEventArgs e)
            {
                await Task.Yield();
                if (e.Message.Id == message_id)
                {
                    if (user == null || e.User.Id == user?.Id)
                    {
                        var rc = new ReactionContext()
                        {
                            Channel = e.Channel,
                            Emoji = e.Emoji,
                            Message = e.Message,
                            User = e.User,
                            Interactivity = this
                        };
                        tsc.TrySetResult(rc);
                        return;
                    }
                }
            }
            #endregion
        }

        public async Task<ReactionCollectionContext> CreatePollAsync(DiscordMessage m, IEnumerable<DiscordEmoji> Emojis, TimeSpan? timeoutoverride = null)
        {
            TimeSpan timeout = Config.Timeout;
            if (timeoutoverride != null)
                timeout = (TimeSpan)timeoutoverride;

            foreach (var em in Emojis)
            {
                await m.CreateReactionAsync(em);
            }

            var rcc = new ReactionCollectionContext();
            var tsc = new TaskCompletionSource<ReactionCollectionContext>();
            var ct = new CancellationTokenSource(timeout);
            ct.Token.Register(() => tsc.TrySetResult(rcc));

            try
            {
                this.Client.MessageReactionAdded += ReactionAddHandler;
                this.Client.MessageReactionRemoved += ReactionRemoveHandler;
                this.Client.MessageReactionsCleared += ReactionClearHandler;

                var result = await tsc.Task;
                return result;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                this.Client.MessageReactionAdded -= ReactionAddHandler;
                this.Client.MessageReactionRemoved -= ReactionRemoveHandler;
                this.Client.MessageReactionsCleared -= ReactionClearHandler;
            }

            #region Handlers
            async Task ReactionAddHandler(MessageReactionAddEventArgs e)
            {
                await Task.Yield();
                if (e.Message.Id == m.Id && Emojis.Count(x => x == e.Emoji) > 0)
                {
                    rcc.AddReaction(e.Emoji, e.User.Id);
                }
            }

            async Task ReactionRemoveHandler(MessageReactionRemoveEventArgs e)
            {
                await Task.Yield();
                if (e.Message.Id == m.Id && Emojis.Count(x => x == e.Emoji) > 0)
                {
                    rcc.RemoveReaction(e.Emoji, e.User.Id);
                }
            }

            async Task ReactionClearHandler(MessageReactionsClearEventArgs e)
            {
                await Task.Yield();
                if (e.Message.Id == m.Id)
                {
                    rcc.ClearReactions();
                    foreach (var em in Emojis)
                    {
                        await m.CreateReactionAsync(em);
                    }
                }
            }
            #endregion
        }

        public async Task<ReactionCollectionContext> CollectReactionsAsync(DiscordMessage m, TimeSpan? timeoutoverride = null)
        {
            TimeSpan timeout = Config.Timeout;
            if (timeoutoverride != null)
                timeout = (TimeSpan)timeoutoverride;

            var rcc = new ReactionCollectionContext();
            var tsc = new TaskCompletionSource<ReactionCollectionContext>();
            var ct = new CancellationTokenSource(timeout);
            ct.Token.Register(() => tsc.TrySetResult(rcc));

            try
            {
                this.Client.MessageReactionAdded += ReactionAddHandler;
                this.Client.MessageReactionRemoved += ReactionRemoveHandler;
                this.Client.MessageReactionsCleared += ReactionClearHandler;

                var result = await tsc.Task;
                return result;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                this.Client.MessageReactionAdded -= ReactionAddHandler;
                this.Client.MessageReactionRemoved -= ReactionRemoveHandler;
                this.Client.MessageReactionsCleared -= ReactionClearHandler;
            }

            #region Handlers
            async Task ReactionAddHandler(MessageReactionAddEventArgs e)
            {
                await Task.Yield();
                if (e.Message.Id == m.Id)
                {
                    rcc.AddReaction(e.Emoji);
                }
            }

            async Task ReactionRemoveHandler(MessageReactionRemoveEventArgs e)
            {
                await Task.Yield();
                if (e.Message.Id == m.Id)
                {
                    rcc.RemoveReaction(e.Emoji);
                }
            }

            async Task ReactionClearHandler(MessageReactionsClearEventArgs e)
            {
                await Task.Yield();
                if (e.Message.Id == m.Id)
                {
                    rcc.ClearReactions();
                }
            }
            #endregion
        }
        #endregion

        #region Typing
        // I don't really know anymore why I added this.. -Naam
        // I think I told you it might be useful, but tbh I have no idea myself -Emzi
        // Did you? I don't remember either. Nice it's there anyway I guess.. -Naam
        public async Task<TypingContext> WaitForTypingUserAsync(DiscordChannel channel, TimeSpan? timeoutoverride = null)
        {
            TimeSpan timeout = Config.Timeout;
            if (timeoutoverride != null)
                timeout = (TimeSpan)timeoutoverride;

            var channel_id = channel.Id;
            var tsc = new TaskCompletionSource<TypingContext>();
            var ct = new CancellationTokenSource(timeout);
            ct.Token.Register(() => tsc.TrySetResult(null));

            try
            {
                this.Client.TypingStarted += Handler;

                var result = await tsc.Task;
                return result;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                this.Client.TypingStarted -= Handler;
            }

            #region Handler
            async Task Handler(TypingStartEventArgs e)
            {
                await Task.Yield();
                if (e.Channel.Id == channel_id)
                {
                    var tc = new TypingContext()
                    {
                        Channel = e.Channel,
                        Interactivity = this,
                        StartedAt = e.StartedAt,
                        User = e.User
                    };
                    tsc.TrySetResult(tc);
                    return;
                }
            }
            #endregion
        }

        public async Task<TypingContext> WaitForTypingChannelAsync(DiscordUser user, TimeSpan? timeoutoverride = null)
        {
            TimeSpan timeout = Config.Timeout;
            if (timeoutoverride != null)
                timeout = (TimeSpan)timeoutoverride;

            var user_id = user.Id;
            var tsc = new TaskCompletionSource<TypingContext>();
            var ct = new CancellationTokenSource(timeout);
            ct.Token.Register(() => tsc.TrySetResult(null));

            AsyncEventHandler<TypingStartEventArgs> handler = async (e) =>
            {
                await Task.Yield();
                if (e.User.Id == user_id)
                {
                    var tc = new TypingContext()
                    {
                        Channel = e.Channel,
                        Interactivity = this,
                        StartedAt = e.StartedAt,
                        User = e.User
                    };
                    tsc.TrySetResult(tc);
                    return;
                }
            };

            try
            {
                this.Client.TypingStarted += handler;

                var result = await tsc.Task;
                return result;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                this.Client.TypingStarted -= handler;
            }
        }
        #endregion

        #region Pagination
        public async Task SendPaginatedMessage(DiscordChannel channel, DiscordUser user, IEnumerable<Page> message_pages, TimeSpan? timeoutoverride = null, TimeoutBehaviour? timeoutbehaviouroverride = null)
        {
            TimeSpan timeout = Config.Timeout;
            if (timeoutoverride != null)
                timeout = (TimeSpan)timeoutoverride;

            TimeoutBehaviour timeout_behaviour = Config.PaginationBehaviour;
            if (timeoutbehaviouroverride != null)
                timeout_behaviour = (TimeoutBehaviour)timeoutbehaviouroverride;

            List<Page> pages = message_pages.ToList();

            if (pages.Count() == 0)
                throw new ArgumentException("You need to provide at least 1 page!");

            var tsc = new TaskCompletionSource<string>();
            var ct = new CancellationTokenSource(timeout);
            ct.Token.Register(() => tsc.TrySetResult(null));

            DiscordMessage m = await this.Client.SendMessageAsync(channel, string.IsNullOrEmpty(pages.First().Content) ? "" : pages.First().Content, embed: pages.First().Embed);
            PaginatedMessage pm = new PaginatedMessage()
            {
                CurrentIndex = 0,
                Pages = pages,
                Timeout = timeout
            };

            await this.GeneratePaginationReactions(m);

            try
            {
                this.Client.MessageReactionsCleared += ReactionClearHandler;
                this.Client.MessageReactionAdded += ReactionAddHandler;
                this.Client.MessageReactionRemoved += ReactionRemoveHandler;

                await tsc.Task;

                switch (timeout_behaviour)
                {
                    case TimeoutBehaviour.Default:
                    case TimeoutBehaviour.Ignore:
                        await m.DeleteAllReactionsAsync();
                        break;
                    case TimeoutBehaviour.Delete:
                        await m.DeleteAllReactionsAsync();
                        await m.DeleteAsync();
                        break;
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                this.Client.MessageReactionsCleared -= ReactionClearHandler;
                this.Client.MessageReactionAdded -= ReactionAddHandler;
                this.Client.MessageReactionRemoved -= ReactionRemoveHandler;
            }

            #region Handlers
            async Task ReactionAddHandler(MessageReactionAddEventArgs e)
            {
                if (e.Message.Id == m.Id && e.User.Id != this.Client.CurrentUser.Id && e.User.Id == user.Id)
                {
                    await this.DoPagination(e.Emoji, m, pm, ct);
                }
            }

            async Task ReactionRemoveHandler(MessageReactionRemoveEventArgs e)
            {
                if (e.Message.Id == m.Id && e.User.Id != this.Client.CurrentUser.Id && e.User.Id == user.Id)
                    await this.DoPagination(e.Emoji, m, pm, ct);
            }

            async Task ReactionClearHandler(MessageReactionsClearEventArgs e)
            {
                await this.GeneratePaginationReactions(m);
            }
            #endregion
        }

        public IEnumerable<Page> GeneratePagesInEmbeds(string input)
        {
            List<Page> result = new List<Page>();
            List<string> split = input.Split(2000).ToList();
            int page = 1;
            foreach (string s in split)
            {
                result.Add(new Page()
                {
                    Embed = new DiscordEmbed()
                    {
                        Title = $"Page {page}",
                        Description = s
                    }
                });
                page++;
            }
            return result;
        }

        public IEnumerable<Page> GeneratePagesInStrings(string input)
        {
            List<Page> result = new List<Page>();
            List<string> split = input.Split(1900).ToList();
            int page = 1;
            foreach (string s in split)
            {
                result.Add(new Page()
                {
                    Content = $"**Page {page}:**\n\n" + s
                });
                page++;
            }
            return result;
        }

        public async Task GeneratePaginationReactions(DiscordMessage m)
        {
            await m.CreateReactionAsync(DiscordEmoji.FromUnicode(this.Client, "⏮"));
            await m.CreateReactionAsync(DiscordEmoji.FromUnicode(this.Client, "◀"));
            await m.CreateReactionAsync(DiscordEmoji.FromUnicode(this.Client, "⏹"));
            await m.CreateReactionAsync(DiscordEmoji.FromUnicode(this.Client, "▶"));
            await m.CreateReactionAsync(DiscordEmoji.FromUnicode(this.Client, "⏭"));
        }

        public async Task DoPagination(DiscordEmoji emoji, DiscordMessage m, PaginatedMessage pm, CancellationTokenSource ct)
        {
            #region The "good" shit
            switch (emoji.Name)
            {
                case "⏮":
                    pm.CurrentIndex = 0;
                    break;

                case "◀":
                    if (pm.CurrentIndex != 0)
                        pm.CurrentIndex--;
                    break;

                case "⏹":
                    ct.Cancel();
                    return;

                case "▶":
                    if (pm.CurrentIndex != pm.Pages.Count() - 1)
                        pm.CurrentIndex++;
                    break;

                case "⏭":
                    pm.CurrentIndex = pm.Pages.Count() - 1;
                    break;

                default:
                    return;
            }

            await m.ModifyAsync((string.IsNullOrEmpty(pm.Pages.ToArray()[pm.CurrentIndex].Content)) ? "" : pm.Pages.ToArray()[pm.CurrentIndex].Content,
                embed: pm.Pages.ToArray()[pm.CurrentIndex].Embed ?? null);
            #endregion
        }
        #endregion
    }

    public enum TimeoutBehaviour
    {
        Default, // ignore
        Ignore,
        Delete
    }

    public class PaginatedMessage
    {
        public IEnumerable<Page> Pages { get; internal set; }
        public int CurrentIndex { get; internal set; }
        public TimeSpan Timeout { get; internal set; }
    }

    public class Page
    {
        public string Content { get; set; }
        public DiscordEmbed Embed { get; set; }
    }

}
// send nudes

// wait don't im not 18 yet

// I mean I don't mind..

// comeon send those nudes man

// 2 months and its legal