/*
 * The MIT License
 *
 * Copyright (C) 2013-2023 Alexander Saprykin <saprykin.spb@gmail.com>
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * 'Software'), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED 'AS IS', WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
 * CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

#include "ptimeprofiler.h"
#include "ptimeprofiler-private.h"

#include <time.h>

puint64
p_time_profiler_get_ticks_internal ()
{
	pint64 val;

	if (P_UNLIKELY ((val = (pint64) time (NULL)) == -1)) {
		P_ERROR ("PTimeProfiler::p_time_profiler_get_ticks_internal: time() failed");
		return 0;
	}

	return (puint64) (val * 1000000);
}

puint64
p_time_profiler_elapsed_usecs_internal (const PTimeProfiler *profiler)
{
	return p_time_profiler_get_ticks_internal () - profiler->counter;
}

void
p_time_profiler_init (void)
{
	/* Not required */
}

void
p_time_profiler_shutdown (void)
{
	/* Not required */
}
