using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices
{
    /// <summary> 表示生成器，用于不返回值的异步方法。</summary>
    public struct AsyncVoidMethodBuilder : IAsyncMethodBuilder
    {
        private readonly SynchronizationContext m_synchronizationContext;

        private AsyncMethodBuilderCore m_coreState;

        private object m_objectIdForDebugger;

        private static int s_preventUnobservedTaskExceptionsInvoked;

        private object ObjectIdForDebugger
        {
            get
            {
                if (this.m_objectIdForDebugger == null)
                {
                    this.m_objectIdForDebugger = new object();
                }
                return this.m_objectIdForDebugger;
            }
        }

        static AsyncVoidMethodBuilder()
        {
            try
            {
                AsyncVoidMethodBuilder.PreventUnobservedTaskExceptions();
            }
            catch
            {
            }
        }

        internal static void PreventUnobservedTaskExceptions()
        {
            if (Interlocked.CompareExchange(ref AsyncVoidMethodBuilder.s_preventUnobservedTaskExceptionsInvoked, 1, 0) == 0)
            {
                TaskScheduler.UnobservedTaskException += (s, e) => { e.SetObserved(); };
            }
        }

        /// <summary>创建类实例</summary>
        /// <returns></returns>
        public static AsyncVoidMethodBuilder Create()
        {
            return new AsyncVoidMethodBuilder(SynchronizationContext.Current);
        }

        private AsyncVoidMethodBuilder(SynchronizationContext synchronizationContext)
        {
            this.m_synchronizationContext = synchronizationContext;
            if (synchronizationContext != null)
            {
                synchronizationContext.OperationStarted();
            }
            this.m_coreState = default(AsyncMethodBuilderCore);
            this.m_objectIdForDebugger = null;
        }

        /// <summary>开始运行有关联状态机的生成器。</summary>
        /// <typeparam name="TStateMachine"></typeparam>
        /// <param name="stateMachine"></param>
        [DebuggerStepThrough]
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            this.m_coreState.Start<TStateMachine>(ref stateMachine);
        }

        /// <summary>一个生成器与指定的状态机关联。</summary>
        /// <param name="stateMachine"></param>
        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
            this.m_coreState.SetStateMachine(stateMachine);
        }

        void IAsyncMethodBuilder.PreBoxInitialization()
        {
        }

        /// <summary>指定的 awaiter 完成时，安排状态机，以继续下一操作。</summary>
        /// <typeparam name="TAwaiter"></typeparam>
        /// <typeparam name="TStateMachine"></typeparam>
        /// <param name="awaiter"></param>
        /// <param name="stateMachine"></param>
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            try
            {
                Action completionAction = this.m_coreState.GetCompletionAction<AsyncVoidMethodBuilder, TStateMachine>(ref this, ref stateMachine);
                awaiter.OnCompleted(completionAction);
            }
            catch (Exception exception)
            {
                AsyncServices.ThrowAsync(exception, null);
            }
        }

        /// <summary>指定的 awaiter 完成时，安排状态机，以继续下一操作。此方法可从部分受信任的代码调用。</summary>
        /// <typeparam name="TAwaiter"></typeparam>
        /// <typeparam name="TStateMachine"></typeparam>
        /// <param name="awaiter"></param>
        /// <param name="stateMachine"></param>
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            try
            {
                Action completionAction = this.m_coreState.GetCompletionAction<AsyncVoidMethodBuilder, TStateMachine>(ref this, ref stateMachine);
                awaiter.UnsafeOnCompleted(completionAction);
            }
            catch (Exception exception)
            {
                AsyncServices.ThrowAsync(exception, null);
            }
        }

        /// <summary>标记此方法生成器为成功完成。</summary>
        public void SetResult()
        {
            if (this.m_synchronizationContext != null)
            {
                this.NotifySynchronizationContextOfCompletion();
            }
        }

        /// <summary>将一个异常绑定到该方法生成器。</summary>
        /// <param name="exception"></param>
        public void SetException(Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException("exception");
            }
            if (this.m_synchronizationContext != null)
            {
                try
                {
                    AsyncServices.ThrowAsync(exception, this.m_synchronizationContext);
                    return;
                }
                finally
                {
                    this.NotifySynchronizationContextOfCompletion();
                }
            }
            AsyncServices.ThrowAsync(exception, null);
        }

        private void NotifySynchronizationContextOfCompletion()
        {
            try
            {
                this.m_synchronizationContext.OperationCompleted();
            }
            catch (Exception exception)
            {
                AsyncServices.ThrowAsync(exception, null);
            }
        }
    }
}
