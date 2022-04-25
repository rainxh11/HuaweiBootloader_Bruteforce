using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HuaweiBootloader.Bruteforce
{

    public class ImeiUnlocker
    {

        private readonly string _imei;
        private readonly long _initialImei;
        private readonly long _checksum;
        private readonly string _path;
        private long _currentOemCode;
        private TimeSpan _timeOut;
        private ImeiUnlocker(string imei, long startingImei, string fastbootPath, TimeSpan timeOut)
        {
            _imei = imei;
            _initialImei = startingImei;
            _checksum = CheckLuhn(imei);
            _path = fastbootPath;
            _currentOemCode = _initialImei;
            _timeOut = timeOut;
        }
        public static long CheckLuhn(string imei)
        {
            int sum = 0;
            bool alternate = false;
            for (int i = imei.Length - 1; i >= 0; i--)
            {
                char[] nx = imei.ToArray();
                int n = int.Parse(nx[i].ToString());

                if (alternate)
                {
                    n *= 2;

                    if (n > 9)
                    {
                        n = (n % 10) + 1;
                    }
                }
                sum += n;
                alternate = !alternate;
            }
            return (sum % 10);
        }
        public static ImeiUnlocker Create(string fastbootPath, string imei, long startingImei = 1_000_000_000_000_000, int timeOut = 10000)
        {
            return new ImeiUnlocker(imei, startingImei, fastbootPath, TimeSpan.FromMilliseconds(timeOut));
        }

        public IEnumerable<long> EnumerateOemCodes()
        {
            yield return _currentOemCode;
            while (_currentOemCode.ToString().Length == 16)
            {
                _currentOemCode += (long)(_checksum + Math.Sqrt(Convert.ToInt64(_imei)) * 1024);
                yield return _currentOemCode;
            }
        }

        public async Task<string> Unlock(string oemCode)
        {
            var startInfo = new ProcessStartInfo(_path)
            {
                Arguments = $"oem unlock {oemCode}",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            try
            {
                var process = Process.Start(startInfo);
                using (var streamReader = process.StandardOutput)
                {
                    var stdOut = await streamReader.ReadToEndAsync().WaitAsync(_timeOut);
                    var wait = process.WaitForExit(_timeOut.Milliseconds);
                    if (!wait)
                    {
                        process.Kill();
                        return null;
                    }
                    return stdOut;
                }
            }
            catch
            {
                return null;

            }

        }

        public IObservable<UnlockResponse> StartUnlock()
        {
            var observable = Observable.Create<UnlockResponse>(async (observer) =>
            {
                var semaphore = new SemaphoreSlim(1);
                var oemEnumerator = this.EnumerateOemCodes().GetEnumerator();
                try
                {
                    do
                    {
                        await semaphore.WaitAsync();
                        var stdOut = await Unlock(oemEnumerator.Current.ToString());
                        semaphore.Release();
                        observer.OnNext(new UnlockResponse(stdOut, oemEnumerator.Current));
                    } while (oemEnumerator.MoveNext());

                    observer.OnCompleted();
                }
                catch (Exception ex)
                {
                    observer.OnError(ex);
                }
                finally
                {
                    oemEnumerator.Dispose();
                    semaphore.Dispose();
                }
            });
            return Observable.Defer(() => observable);
        }

        public async IAsyncEnumerable<UnlockResponse> EnumerateUnlock()
        {
            foreach (var oemCode in EnumerateOemCodes())
            {
                var stdOut = await this.Unlock(oemCode.ToString());
                if (string.IsNullOrEmpty(stdOut))
                    yield return new UnlockResponse($"Fastboot TimeOut!", oemCode);

                yield return new UnlockResponse(stdOut, oemCode);
            }
        }
    }

    public record UnlockResponse(string FastbootStdOutput, long OemCode);
}
