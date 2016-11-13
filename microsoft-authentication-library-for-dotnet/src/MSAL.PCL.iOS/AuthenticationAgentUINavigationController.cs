//------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System.Collections.Generic;
using CoreGraphics;
using UIKit;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// </summary>
    [Foundation.Register("UniversalView")]
    public class UniversalView : UIView
    {
        /// <summary>
        /// </summary>
        public UniversalView()
        {
            Initialize();
        }

        /// <summary>
        /// </summary>
        public UniversalView(CGRect bounds)
            : base(bounds)
        {
            Initialize();
        }

        private void Initialize()
        {
            BackgroundColor = UIColor.Red;
        }
    }

    [Foundation.Register("AuthenticationAgentUINavigationController")]
    internal class AuthenticationAgentUINavigationController : UINavigationController
    {
        private readonly IDictionary<string, string> additionalHeaders;
        private readonly string callback;
        private readonly AuthenticationAgentUIViewController.ReturnCodeCallback callbackMethod;
        private readonly string url;

        /// <summary>
        /// </summary>
        public AuthenticationAgentUINavigationController(string url, string callback,
            IDictionary<string, string> additionalHeaders,
            AuthenticationAgentUIViewController.ReturnCodeCallback callbackMethod)
        {
            this.url = url;
            this.callback = callback;
            this.callbackMethod = callbackMethod;
            this.additionalHeaders = additionalHeaders;
        }

        public override void DidReceiveMemoryWarning()
        {
            // Releases the view if it doesn't have a superview.
            base.DidReceiveMemoryWarning();

            // Release any cached data, images, etc that aren't in use.
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            // Perform any additional setup after loading the view
            CustomHeaderHandler.AdditionalHeaders = this.additionalHeaders;
            this.PushViewController(
                new AuthenticationAgentUIViewController(this.url, this.callback, this.callbackMethod), true);
        }
    }
}