/*
 * The MIT License
 *
 * Copyright (C) 2010-2023 Alexander Saprykin <saprykin.spb@gmail.com>
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

#include "pmem.h"
#include "pcondvariable.h"

#include <stdlib.h>
#include <string.h>
#include <time.h>
#include <pthread.h>

struct PCondVariable_ {
	pthread_cond_t hdl;
};

P_LIB_API PCondVariable *
p_cond_variable_new (void)
{
	PCondVariable *ret;

	if (P_UNLIKELY ((ret = p_malloc0 (sizeof (PCondVariable))) == NULL)) {
		P_ERROR ("PCondVariable::p_cond_variable_new: failed to allocate memory");
		return NULL;
	}

	if (P_UNLIKELY (pthread_cond_init (&ret->hdl, NULL) != 0)) {
		P_ERROR ("PCondVariable::p_cond_variable_new: failed to initialize");
		p_free (ret);
		return NULL;
	}

	return ret;
}

P_LIB_API void
p_cond_variable_free (PCondVariable *cond)
{
	if (P_UNLIKELY (cond == NULL))
		return;

	if (P_UNLIKELY (pthread_cond_destroy (&cond->hdl) != 0))
		P_WARNING ("PCondVariable::p_cond_variable_free: pthread_cond_destroy() failed");

	p_free (cond);
}

P_LIB_API pboolean
p_cond_variable_wait (PCondVariable	*cond,
		      PMutex		*mutex)
{
	if (P_UNLIKELY (cond == NULL || mutex == NULL))
		return FALSE;

	/* Cast is eligible since there is only one field in the PMutex structure */
	if (P_UNLIKELY (pthread_cond_wait (&cond->hdl, (pthread_mutex_t *) mutex) != 0)) {
		P_ERROR ("PCondVariable::p_cond_variable_wait: pthread_cond_wait() failed");
		return FALSE;
	}

	return TRUE;
}

P_LIB_API pboolean
p_cond_variable_signal (PCondVariable *cond)
{
	if (P_UNLIKELY (cond == NULL))
		return FALSE;

	if (P_UNLIKELY (pthread_cond_signal (&cond->hdl) != 0)) {
		P_ERROR ("PCondVariable::p_cond_variable_signal: pthread_cond_signal() failed");
		return FALSE;
	}

	return TRUE;
}

P_LIB_API pboolean
p_cond_variable_broadcast (PCondVariable *cond)
{
	if (P_UNLIKELY (cond == NULL))
		return FALSE;

	if (P_UNLIKELY (pthread_cond_broadcast (&cond->hdl) != 0)) {
		P_ERROR ("PCondVariable::p_cond_variable_broadcast: thread_cond_broadcast() failed");
		return FALSE;
	}

	return TRUE;
}

void
p_cond_variable_init (void)
{
	/* Not required */
}

void
p_cond_variable_shutdown (void)
{
	/* Not required */
}
