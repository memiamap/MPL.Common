﻿using MPL.Common.Win32.WinSCard;
using System;

namespace MPL.Common.Win32
{
    /// <summary>
    /// A class that defines WinSCard functions.
    /// </summary>
    public static class WinSCardIntegration
    {
        #region Methods
        #region _Private_
        private static SCARD_READERSTATE CreateReaderState(string name)
        {
            SCARD_READERSTATE returnValue;

            returnValue = new SCARD_READERSTATE
            {
                szReader = name,
                dwCurrentState = (uint)SCardReaderEventState.SCARD_STATE_UNAWARE
            };

            return returnValue;
        }

        private static SCARD_READERSTATE[] CreateReaderStates(string[] readers, bool discoverNewDevices = false)
        {
            int offset = 0;
            SCARD_READERSTATE[] returnValue;

            if (discoverNewDevices)
            {
                offset = 1;
                returnValue = new SCARD_READERSTATE[readers.Length + 1];
                returnValue[0] = CreateReaderState(@"\\?PnP?\Notification");
            }
            else
                returnValue = new SCARD_READERSTATE[readers.Length];

            for (int i = 0; i < returnValue.Length; i++)
                returnValue[i + offset] = CreateReaderState(readers[i]);

            return returnValue;
        }

        private static SCARD_READERSTATE[] GetReaderStates(IntPtr context, string[] readers)
        {
            SCARD_READERSTATE[] returnValue;

            if (context != null && context != IntPtr.Zero)
            {
                SCardResponse result;
                bool wasSuccessful;

                returnValue = CreateReaderStates(readers);

                try
                {
                    wasSuccessful = NativeMethods.GetStatusChange(context, returnValue, 500, out result);
                }
                catch (Exception ex)
                {
                    throw new Exception("Unable to get the states due to an exception", ex);
                }

                if (!wasSuccessful)
                    throw new InvalidOperationException($"Unable to get the states. Error code {result}");
            }
            else
                throw new ArgumentException("The specified context is invalid", nameof(context));

            return returnValue;
        }

        private static string[] ListReadersInternal(IntPtr context, SCardReaderGroup group)
        {
            string[] returnValue;

            if (context != null && context != IntPtr.Zero)
            {
                SCardResponse result;
                bool wasSuccessful;

                try
                {
                    wasSuccessful = NativeMethods.ListReaders(context, group, out returnValue, out result);
                }
                catch (Exception ex)
                {
                    throw new Exception("Unable to list readers due to an exception", ex);
                }

                if (!wasSuccessful)
                {
                    if (result == SCardResponse.SCARD_E_NO_READERS_AVAILABLE)
                        throw new InvalidOperationException($"Unable to list readers. No readers are available");
                    else if (result == SCardResponse.SCARD_E_READER_UNAVAILABLE)
                        throw new InvalidOperationException($"Unable to list readers. The specified reader is currently unavailable");
                    else
                        throw new InvalidOperationException($"Unable to list readers. Error code {result}");
                }
            }
            else
                throw new ArgumentException("The specified context is invalid", nameof(context));

            return returnValue;
        }

        #endregion
        #region _Public_
        /// <summary>
        /// Attempts to create a Winscard context.
        /// </summary>
        /// <returns>An IntPtr that is the created context.</returns>
        public static IntPtr CreateContext()
        {
            SCardResponse result;
            IntPtr returnValue;
            bool wasSuccessful;

            try
            {
                wasSuccessful = NativeMethods.EstablishContext(out returnValue, out result);
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to create the context due to an exception", ex);
            }

            if (!wasSuccessful)
                throw new InvalidOperationException($"Unable to create the context. Error code {result}");

            return returnValue;
        }

        /// <summary>
        /// Attempts to destroy a Winscard context.
        /// </summary>
        /// <param name="context">An IntPtr that is the context to destroy.</param>
        public static void DestroyContext(IntPtr context)
        {
            if (context != null && context != IntPtr.Zero)
            {
                SCardResponse result;
                bool wasSuccessful;

                try
                {
                    wasSuccessful = NativeMethods.ReleaseContext(context, out result);
                }
                catch (Exception ex)
                {
                    throw new Exception("Unable to destroy the context due to an exception", ex);
                }

                if (!wasSuccessful)
                    throw new InvalidOperationException($"Unable to destroy the context. Error code {result}");
            }
            else
                throw new ArgumentException("The specified context is invalid", nameof(context));
        }

        /// <summary>
        /// Gets the data of the specified readers.
        /// </summary>
        /// <param name="context">An IntPtr that is the context to use.</param>
        /// <param name="reader">A string containing the name of the reader to get the data of.</param>
        /// <returns>An array of byte that is the data from the reader.</returns>
        public static byte[] GetReaderData(IntPtr context, string reader)
        {
            byte[] returnValue;
            SCARD_READERSTATE[] states;

            states = GetReaderStates(context, new[] { reader });
            if (states.Length == 1 && states[0].rgbAtr != null)
            {
                returnValue = new byte[states[0].rgbAtr.Length];
                Array.Copy(states[0].rgbAtr, 0, returnValue, 0, returnValue.Length);
            }
            else
                throw new ArgumentException("Unable to obtain the data for the specified reader", nameof(reader));

            return returnValue;
        }

        /// <summary>
        /// Gets the status of the specified reader.
        /// </summary>
        /// <param name="context">An IntPtr that is the context to use.</param>
        /// <param name="reader">A string containing the name of the reader to get the status of.</param>
        /// <returns>A SCardReaderEventState that is the status of the reader.</returns>
        public static SCardReaderEventState GetReaderStatus(IntPtr context, string reader)
        {
            return GetReaderStatus(context, new[] { reader })[0];
        }
        /// <summary>
        /// Gets the status of the specified readers.
        /// </summary>
        /// <param name="context">An IntPtr that is the context to use.</param>
        /// <param name="readers">An array of string containing the name of the readers to get the status of.</param>
        /// <returns>An array of SCardReaderEventState that is the status of the readers.</returns>
        public static SCardReaderEventState[] GetReaderStatus(IntPtr context, string[] readers)
        {
            SCardReaderEventState[] returnValue;
            SCARD_READERSTATE[] states;

            states = GetReaderStates(context, readers);
            returnValue = new SCardReaderEventState[states.Length];
            for (int i = 0; i < returnValue.Length; i++)
                returnValue[i] = (SCardReaderEventState)((int)states[i].dwEventState & 0xFFFF);

            return returnValue;
        }

        /// <summary>
        /// Lists all readers using the specified context.
        /// </summary>
        /// <param name="context">An IntPtr that is the context to use.</param>
        /// <returns>An array of string containing the list of readers.</returns>
        public static string[] ListAllReaders(IntPtr context)
        {
            return ListReadersInternal(context, SCardReaderGroup.AllReaders);
        }

        /// <summary>
        /// Lists default readers using the specified context.
        /// </summary>
        /// <param name="context">An IntPtr that is the context to use.</param>
        /// <returns>An array of string containing the list of readers.</returns>
        public static string[] ListDefaultReaders(IntPtr context)
        {
            return ListReadersInternal(context, SCardReaderGroup.DefaultReaders);
        }

        #endregion
        #endregion
    }
}