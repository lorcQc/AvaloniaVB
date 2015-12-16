﻿// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using Microsoft.Reactive.Testing;
using Xunit;

namespace Perspex.Styling.UnitTests
{
    public class StyleActivatorTests : ReactiveTest
    {
        [Fact]
        public void Activator_Should_Subscribe_To_Inputs_On_First_Subscription()
        {
            var scheduler = new TestScheduler();
            var source = scheduler.CreateColdObservable<bool>();
            var target = new StyleActivator(new[] { source }, ActivatorMode.And);

            Assert.Equal(0, source.Subscriptions.Count);
            target.Subscribe(_ => { });
            Assert.Equal(1, source.Subscriptions.Count);
        }

        [Fact]
        public void Activator_Should_Unsubscribe_From_Inputs_After_Last_Subscriber_Completes()
        {
            var scheduler = new TestScheduler();
            var source = scheduler.CreateColdObservable<bool>();
            var target = new StyleActivator(new[] { source }, ActivatorMode.And);

            var dispose = target.Subscribe(_ => { });
            Assert.Equal(1, source.Subscriptions.Count);
            Assert.Equal(Subscription.Infinite, source.Subscriptions[0].Unsubscribe);

            dispose.Dispose();
            Assert.Equal(1, source.Subscriptions.Count);
            Assert.Equal(0, source.Subscriptions[0].Unsubscribe);
        }

        [Fact]
        public void Activator_And_Should_Follow_Single_Input()
        {
            var inputs = new[] { new TestSubject<bool>(false) };
            var target = new StyleActivator(inputs, ActivatorMode.And);
            var result = new TestObserver<bool>();

            target.Subscribe(result);
            Assert.False(result.GetValue());
            inputs[0].OnNext(true);
            Assert.True(result.GetValue());
            inputs[0].OnNext(false);
            Assert.False(result.GetValue());
            inputs[0].OnNext(true);
            Assert.True(result.GetValue());

            Assert.Equal(1, inputs[0].SubscriberCount);
        }

        [Fact]
        public void Activator_And_Should_AND_Multiple_Inputs()
        {
            var inputs = new[]
            {
                new TestSubject<bool>(false),
                new TestSubject<bool>(false),
                new TestSubject<bool>(true),
            };
            var target = new StyleActivator(inputs, ActivatorMode.And);
            var result = new TestObserver<bool>();

            target.Subscribe(result);
            Assert.False(result.GetValue());
            inputs[0].OnNext(true);
            inputs[1].OnNext(true);
            Assert.True(result.GetValue());
            inputs[0].OnNext(false);
            Assert.False(result.GetValue());

            Assert.Equal(1, inputs[0].SubscriberCount);
            Assert.Equal(1, inputs[1].SubscriberCount);
            Assert.Equal(1, inputs[2].SubscriberCount);
        }

        [Fact]
        public void Activator_And_Should_Not_Unsubscribe_All_When_Input_Completes_On_True()
        {
            var inputs = new[]
            {
                new TestSubject<bool>(false),
                new TestSubject<bool>(false),
                new TestSubject<bool>(true),
            };
            var target = new StyleActivator(inputs, ActivatorMode.And);
            var result = new TestObserver<bool>();

            target.Subscribe(result);
            Assert.False(result.GetValue());
            inputs[0].OnNext(true);
            inputs[0].OnCompleted();

            Assert.Equal(0, inputs[0].SubscriberCount);
            Assert.Equal(1, inputs[1].SubscriberCount);
            Assert.Equal(1, inputs[2].SubscriberCount);
        }

        [Fact]
        public void Activator_Or_Should_Follow_Single_Input()
        {
            var inputs = new[] { new TestSubject<bool>(false) };
            var target = new StyleActivator(inputs, ActivatorMode.Or);
            var result = new TestObserver<bool>();

            target.Subscribe(result);
            Assert.False(result.GetValue());
            inputs[0].OnNext(true);
            Assert.True(result.GetValue());
            inputs[0].OnNext(false);
            Assert.False(result.GetValue());
            inputs[0].OnNext(true);
            Assert.True(result.GetValue());

            Assert.Equal(1, inputs[0].SubscriberCount);
        }

        [Fact]
        public void Activator_Or_Should_OR_Multiple_Inputs()
        {
            var inputs = new[]
            {
                new TestSubject<bool>(false),
                new TestSubject<bool>(false),
                new TestSubject<bool>(true),
            };
            var target = new StyleActivator(inputs, ActivatorMode.Or);
            var result = new TestObserver<bool>();

            target.Subscribe(result);
            Assert.True(result.GetValue());
            inputs[2].OnNext(false);
            Assert.False(result.GetValue());
            inputs[0].OnNext(true);
            Assert.True(result.GetValue());

            Assert.Equal(1, inputs[0].SubscriberCount);
            Assert.Equal(1, inputs[1].SubscriberCount);
            Assert.Equal(1, inputs[2].SubscriberCount);
        }

        [Fact]
        public void Activator_Or_Should_Not_Unsubscribe_All_When_Input_Completes_On_False()
        {
            var inputs = new[]
            {
                new TestSubject<bool>(false),
                new TestSubject<bool>(false),
                new TestSubject<bool>(true),
            };
            var target = new StyleActivator(inputs, ActivatorMode.Or);
            var result = new TestObserver<bool>();

            target.Subscribe(result);
            Assert.True(result.GetValue());
            inputs[2].OnNext(false);
            Assert.False(result.GetValue());
            inputs[2].OnCompleted();

            Assert.Equal(1, inputs[0].SubscriberCount);
            Assert.Equal(1, inputs[1].SubscriberCount);
            Assert.Equal(0, inputs[2].SubscriberCount);
        }

        [Fact]
        public void Completed_Activator_Should_Signal_OnCompleted()
        {
            var inputs = new[]
            {
                Observable.Return(false),
            };

            var target = new StyleActivator(inputs, ActivatorMode.Or);
            var completed = false;

            target.Subscribe(_ => { }, () => completed = true);

            Assert.True(completed);
        }

        private Recorded<Notification<bool>>[] OnNextValues(params bool[] values)
        {
            var result = new List<Recorded<Notification<bool>>>();
            var time = 1;

            foreach (var value in values)
            {
                result.Add(new Recorded<Notification<bool>>(time, Notification.CreateOnNext<bool>(value)));
            }

            return result.ToArray();
        }
    }
}
