/*      
 *  Copyright 2014-2017 Saul Alonso Monsalve, Felix Garcia Carballeira, Alejandro Calderon Mateos
 *
 *  This file is part of ComBoS.
 * 
 *  ComBoS is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU Lesser General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  ComBoS is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with ComBoS. If not, see <http://www.gnu.org/licenses/>.
 *
 */


/* BOINC architecture simulator */

#include <assert.h>
#include <stdint.h>
#include <stdio.h>
#include <locale.h>		// Big numbers nice output
#include <math.h>
#include <inttypes.h>
#include "msg/msg.h"            /* Yeah! If you want to use msg, you need to include msg/msg.h */
#include "xbt/ex.h"
#include "xbt/sysdep.h"         /* calloc, printf */
#include "xbt/synchro_core.h"

/* Create a log channel to have nice outputs. */
#include "xbt/asserts.h"
#include "xbt/queue.h" 
#include "rand.h"

XBT_LOG_NEW_DEFAULT_CATEGORY(boinc_simulator, "Messages specific for this boinc simulator");

#define MAX_SHORT_TERM_DEBT 86400
#define MAX_TIMEOUT_SERVER 86400*365 	// One year without client activity, only to finish simulation for a while
#define MAX_SIMULATED_TIME 1200		// Simulation time in hours
#define WORK_FETCH_PERIOD 60		// Work fetch period
#define MAX_WORK_FETCH_MULTIPLICATOR (86400.0 / WORK_FETCH_PERIOD)
#define KB 1024				// 1 KB in bytes
#define PRECISION 0.00001		// Accuracy (used in client_work_fetch())
#define CREDITS_CPU_S 0.002315 		// Credits per second (1 GFLOP machine)
#define NUMBER_PROJECTS 1		// Number of projects
#define NUMBER_SCHEDULING_SERVERS 1	// Number of scheduling servers
#define NUMBER_DATA_SERVERS 2		// Number of data servers per project
#define NUMBER_CLIENT_GROUPS 1		// Number of groups
#define REQUEST_SIZE 10*KB		// Request size
#define REPLY_SIZE 10*KB		// Reply size 
#define MAX_BUFFER 100		// Max buffer

/* Project back end */
int init_database(int argc, char *argv[]);
int work_generator(int argc, char *argv[]);
int validator(int argc, char *argv[]);
int assimilator(int argc, char *argv[]);

/* Scheduling server */
int scheduling_server_requests(int argc, char *argv[]);
int scheduling_server_dispatcher(int argc, char *argv[]);

/* Data server */
int data_server_requests(int argc, char *argv[]);	
int data_server_dispatcher(int argc, char *argv[]);		

/* Client */
int client_execute_tasks(int argc, char *argv[]);
int client(int argc, char *argv[]);

/* Test all */
msg_error_t test_all(const char *platform_file, const char *application_file);	

/* Types */
typedef enum {ERROR, IN_PROGRESS, VALID} workunit_status;	// Workunit status
typedef enum {REQUEST, REPLY, TERMINATION} message_type;	// Message type
typedef enum {FAIL, SUCCESS} result_status;			// Result status
typedef enum {CORRECT, INCORRECT} result_value;			// Result value
typedef struct ssmessage s_ssmessage_t, *ssmessage_t;		// Message to scheduling server
typedef struct request s_request_t, *request_t;			// Client request to scheduling server
typedef struct reply s_reply_t, *reply_t;			// Client reply to scheduling server
typedef struct result s_result_t, *result_t;			// Result
typedef struct dsmessage s_dsmessage_t, *dsmessage_t;		// Message to data server
typedef struct workunit s_workunit_t, *workunit_t;		// Workunit
typedef struct task s_task_t, *task_t;				// Task
typedef struct project s_project_t, *project_t;			// Project
typedef struct client s_client_t, *client_t;			// Client
typedef struct application s_application_t, *application_t;	// Application
typedef struct project_database s_pdatabase_t, *pdatabase_t;	// Project database
typedef struct scheduling_server s_sserver_t, *sserver_t;	// Scheduling server
typedef struct data_server s_dserver_t, *dserver_t;		// Data server
typedef struct client_group s_group_t, *group_t;		// Client group

/* Message to scheduling server */
struct ssmessage{
	message_type type;	// REQUEST, REPLY, TERMINATION
	void *content;		// Content (request or reply)
};

/* Client request to scheduling server */
struct request{
	char *answer_mailbox;	// Answer mailbox
	int32_t group_speed;	// Client group speed
	int64_t speed;		// Client speed
	double percentage;	// Percentage of project
};

/* Client reply to scheduling server */
struct reply{
	result_status status;	// Result status
	result_value value;	// Result value
	char* workunit;		// Workunit 
	int32_t result_number;	// Result number
	int32_t credits;	// Credits requested
};

/* Result (workunit instance) */
struct result {
	workunit_t workunit;	// Associated workunit
	int32_t ninput_files;	// Number of input files
	char** input_files;	// Input files names (URLs)
	int32_t number_tasks;	// Number of tasks (usually one)
	task_t *tasks;		// Tasks
};

/* Message to data server */
struct dsmessage {
	message_type type;		// REQUEST, REPLY, TERMINATION
	int32_t application;
	char *answer_mailbox;		// Answer mailbox
};

/* Workunit */
struct workunit{
	int32_t application; // Associated application
	char *number;			// Workunit number
	workunit_status status;		// ERROR, IN_PROGRESS, VALID
	char ntotal_results;		// Number of created results
	char nsent_results;		// Number of sent results
	char nresults_received;		// Number of received results
	char nvalid_results;		// Number of valid results
	char nsuccess_results;		// Number of success results
	char nerror_results;		// Number of erroneous results
	char ncurrent_error_results;	// Number of current error results
	int32_t credits;		// Number of credits per valid result
	double* times;			// Time when each result was sent
	int32_t ninput_files;		// Number of input files
	char** input_files;		// Input files names (URLs)
};

/* Client project */
struct project {

	/* General data of project */

	char *name;				// Project name
	char *answer_mailbox;			// Client answer mailbox
	char priority;				// Priority (maximum 255)
	char number;				// Project number (maximum 255)
	char on;				// Project is working

	/* Data to control tasks and their execution */

	task_t running_task;			// which is the task that is running on thread */
	msg_process_t thread;			// thread running the tasks of this project */
	client_t client;			// Client pointer
	xbt_swag_t tasks;			// nearly runnable jobs of project, insert at tail to keep ordered
	xbt_swag_t sim_tasks;			// nearly runnable jobs of project, insert at tail to keep ordered
	xbt_swag_t run_list;			// list of jobs running, normally only one tasks, but if a deadline may occur it can put another here
	xbt_queue_t tasks_ready;		// synchro queue, thread to execute task */
	xbt_queue_t number_executed_task;	// Queue with executed task numbers
	xbt_queue_t workunit_executed_task;	// Cola size tareas ejecutadas

	/* Statistics data */

	int32_t total_tasks_checked;		// (maximum 2³¹-1)
        int32_t total_tasks_executed;		// (maximum 2³¹-1)
	int32_t total_tasks_received;		// (maximum 2³¹-1)
        int32_t total_tasks_missed;		// (maximum 2³¹-1)

	/* Data to simulate boinc scheduling behavior */

	double anticipated_debt;
	double short_debt;
	double long_debt;
	double wall_cpu_time;		// cpu time used by project during most recent period (SchedulingInterval or action finish)
	double shortfall;
};

/* Task */
struct task {
	char *workunit;			// Workunit of the task
	int32_t application;
	char *name;			// Task name
	char scheduled;			// Task scheduled (it is in tasks_ready list) [0,1]
	char running;			// Task is actually running on cpu [0,1]
	s_xbt_swag_hookup_t tasks_hookup;
	s_xbt_swag_hookup_t run_list_hookup;
	s_xbt_swag_hookup_t sim_tasks_hookup;
	msg_task_t msg_task;
	project_t project;		// Project reference
	int64_t heap_index;		// (maximum 2⁶³-1)
	int64_t deadline;		// Task deadline (maximum 2⁶³-1)
	double duration;		// Task duration in flops
	double start;
	double sim_finish;		// Simulated finish time of task
	double sim_remains;
};

/* Client */
struct client {
	const char *name;
	double work_fetch_multiplicator;
	int join_day;
	int type; // type depending on availability: reliable, periodic, usual, unreliable
	xbt_dict_t projects;		// all projects of client
	xbt_heap_t deadline_missed;
	project_t running_project; 
	msg_process_t work_fetch_thread;
	xbt_cond_t sched_cond;
	xbt_mutex_t sched_mutex;
	xbt_cond_t work_fetch_cond;
	xbt_mutex_t work_fetch_mutex;	
	xbt_mutex_t mutex_init;
	xbt_cond_t cond_init;
	xbt_mutex_t ask_for_work_mutex;
	xbt_cond_t ask_for_work_cond;
	char n_projects;		// Number of projects attached
	char finished;			// Number of finished threads (maximum 255)
	char no_actions;		// No actions [0,1]
	char on;			// Client will know who sent the signal
	char initialized;		// Client initialized or not [0,1]
	char stats_online;
	char stats_idle;
	int32_t group_number;		// Group_number
	int64_t speed;			// Host speed
	double sum_priority;		// sum of projects' priority
	double total_shortfall;
	double last_wall;		// last time where the wall_cpu_time was updated
	double factor;			// host speed factor
	double suspended;		// Client is suspended (>0) or not (=0)	
};

/*Project application*/
struct application {
	/* Redundancy and scheduling attributes */

	double percentage; /* Percantage of workunits in the project belonging to this application*/

	int32_t min_quorum;		// Minimum number of successful results required for the validator. If a scrict majority agree, they are considered correct
	int32_t target_nresults;	// Number of results to create initially per workunit
	int32_t max_error_results;	// If the number of client error results exeed this, the workunit is declared to have an error
	int32_t max_total_results;	// If the number of results for this workunit exeeds this, the workunit is declared to be in error
	int32_t max_success_results;	// If the number of success results for this workunit exeeds this, and a consensus has not been reached, the workunit is declared to be in error
	int64_t delay_bound;		// The time by which a result must be completed (deadline)

	/* Results attributes */

	char ifgl_percentage;		// Percentage of input files generated locally
	char ifcd_percentage;			// Number of workunits that share the same input files
	char success_percentage;	// Percentage of success results
	char canonical_percentage;	// Percentage of success results that make up a consensus
	int64_t input_file_size;	// Size of the input files needed for a result associated with a workunit of this project 	
	int64_t output_file_size;	// Size of the output files needed for a result associated with a workunit of this project
	int64_t job_duration;		// Job length in FLOPS

	/* Workunit statistics */

	int64_t nworkunits;		// Number of workunits created
	int64_t nvalid_workunits;	// Number of workunits validated
	int64_t nvalid_workunits_not_finished;
	int64_t nerror_workunits;	// Number of erroneous workunits
	int32_t *valid_workunits_timestamps;
	int32_t *valid_completed_workunits_timestamps;
	int32_t *creation_workunit_timestamps;

	int64_t workunits_number; // Number of workunits generate in one period
	int64_t sleep_time; // Sleep time between periods
	int64_t tail_target_workunits_total;
	int32_t is_on;
	double suspended_until;
	int32_t nworkunits_cur;
};

/* Project database */
struct project_database{

	/* Project attributes */

	char project_number;		// Project number	
	char nscheduling_servers;	// Number of scheduling servers
	char ndata_servers;		// Number of data servers
	char** scheduling_servers;	// Scheduling servers names
	char** data_servers;		// Data servers names
	char *project_name;		// Project name
	int32_t nclients;		// Number of clients	
	int32_t nfinished_clients;	// Number of finished clients
	int64_t disk_bw;		// Disk bandwidth of data servers
	int32_t activate_tail_stage;
	double utilization_safety;
	double theoretical_flops_budget;
	double effective_flops_budget;
	int32_t tail_budget_initialized;

	/*Applications */
	int32_t applications_num;
	application_t applications;

	/* Result statistics */	

	int64_t nmessages_received;	// Number of messages received (requests+replies)
	int64_t nwork_requests;		// Number of requests received
	int64_t nresults;		// Number of created results
	int64_t nresults_sent;		// Number of sent results
	int64_t nvalid_results;		// Number of valid results
	int64_t nresults_received;	// Number of results returned from the clients
	int64_t nresults_analyzed;	// Number of analyzed results
	int64_t nsuccess_results;	// Number of success results
	int64_t nerror_results;		// Number of error results
	int64_t ndelay_results;		// Number of results delayed
	
	/* Workunit statistics */

	int64_t total_credit;		// Total credit granted
	int64_t nworkunits;		// Number of workunits created
	int64_t nvalid_workunits;	// Number of workunits validated
	int64_t nvalid_workunits_not_finished;
	int64_t nerror_workunits;	// Number of erroneous workunits
	int32_t *valid_workunits_timestamps;
	int32_t *valid_completed_workunits_timestamps;
	int32_t *workunit_timestamps;

	/* Clients statistics*/

	int32_t *clients_availability;

	/* Work generator */

	int64_t ncurrent_results;		// Number of results currently in the system
	int64_t ncurrent_error_results;		// Number of error results currently in the system
	xbt_dict_t current_workunits;		// Current workunits
	xbt_queue_t current_results;		// Current results
	xbt_queue_t current_error_results;	// Current error results  
	xbt_mutex_t r_mutex;			// Results mutex
	xbt_mutex_t er_mutex;			// Error results mutex
	xbt_cond_t wg_empty;			// Work generator CV empty
	xbt_cond_t wg_full;			// Work generator CV full
	xbt_cond_t wg_err;	
	int wg_end;				// Work generator end

	/* Validator */

	int64_t ncurrent_validations;		// Number of results currently in the system 
	xbt_queue_t current_validations; 	// Current results
	xbt_mutex_t v_mutex;			// Results mutex
	xbt_cond_t v_empty;			// Validator CV empty
	int v_end;				// Validator end

	/* Assimilator */
	
	int64_t ncurrent_assimilations;		// Number of workunits waiting for assimilation
	xbt_queue_t current_assimilations;	// Current workunits waiting for asimilation
	xbt_mutex_t a_mutex;			// Assimilator mutex
	xbt_cond_t a_empty;			// Assimilator CV empty
	int a_end;				// Assimilator end

	/* Input files */

	int32_t replication;		// Input files replication	
	int64_t ninput_files;		// Number of input files currently in the system
	xbt_queue_t input_files;	// Current input files 
	xbt_mutex_t i_mutex;		// Input files mutex
	xbt_cond_t i_empty;		// Input files CV empty
	xbt_cond_t i_full;		// Input files CV full

	/* Output files */
	
	int64_t noutput_files;		// Number of output files currently in the system
	xbt_queue_t output_files;	// Current output files
	xbt_mutex_t o_mutex;		// Output file mutex
	xbt_cond_t o_empty;		// Onput files CV empty
	xbt_cond_t o_full;		// Onput files CV full
	
	/* Synchronization */
	
	xbt_mutex_t ssrmutex;			// Scheduling server request mutex
	xbt_mutex_t ssdmutex;			// Scheduling server dispatcher mutex
	msg_bar_t barrier;			// Wait until database is initialized	
	char nfinished_scheduling_servers;	// Number of finished scheduling servers
};

/* Scheduling server */
struct scheduling_server{
	char project_number;		// Project number
	const char *server_name;	// Server name
	xbt_mutex_t mutex;		// Mutex
	xbt_cond_t cond;		// VC
	xbt_queue_t client_requests;	// Requests queue
	int32_t EmptyQueue;		// Queue end [0,1]
	int64_t Nqueue;			// Queue size
	double time_busy;		// Time the server is busy
};

/* Data server */
struct data_server{
	char project_number;		// Project number
	const char *server_name;	// Server name
	xbt_mutex_t mutex;		// Mutex
	xbt_cond_t cond;		// VC
	xbt_queue_t client_requests;	// Requests queue
	int32_t EmptyQueue;		// Queue end [0,1]
	int64_t Nqueue;			// Queue size
	double time_busy;		// Time the server is busy
};

/* Client group */
struct client_group{
	char on;			// 0 -> Empty, 1-> proj_args length
	char sp_distri;			// Speed distribution
	char av_distri;			// Availability distribution
	char nv_distri;			// Non-availability distribution
	xbt_mutex_t mutex;		// Mutex
	xbt_cond_t cond;		// Cond
	char **proj_args;		// Arguments
	int32_t group_speed;		// Group speed
	int32_t n_clients;		// Number of clients of the group
	int64_t total_speed;		// Total speed
	double total_available;		// Total time clients available
	double total_notavailable;	// Total time clients not available
	double connection_interval;	
	double scheduling_interval;
	double sa_param;		// Speed A parameter
	double sb_param;		// Speed B parameter	
	double aa_param;		// Availability A parameter
	double ab_param;		// Availability B parameter
	double na_param;		// Non availability A parameter
	double nb_param;		// Non availability B parameter
	double max_speed;		// Maximum host speed
	double min_speed;		// Minimum host speed
	double tail_mean_speed;
	double tail_availability;
};

/* Simulation time */
const double sim_duration = (MAX_SIMULATED_TIME)*3600;	// Simulation time in seconds

/* Server info */
pdatabase_t _pdatabase;			// Projects databases 
sserver_t _sserver_info;		// Scheduling servers information
dserver_t _dserver_info;		// Data servers information 
group_t _group_info;		// Client groups information

/* Synchronization */
xbt_mutex_t _client_mutex;		// Client mutex

/* Asynchronous communication */
xbt_dict_t _sscomm;			// Asynchro communications storage (scheduling server with client)
xbt_dict_t _dscomm;			// Asynchro communications storage (data server with client)

/* Availability statistics */
int32_t _num_clients_t;			// Total number of clients
int64_t _total_speed; 			// Total clients speed (maximum 2⁶³-1)
double _total_available;		// Total time clients available
double _total_notavailable;		// Total time clients notavailable
double *_grid_online_power_deltas;
double *_grid_idle_power_deltas;
xbt_mutex_t _grid_power_mutex;

/* 
 *	Parse memory usage 
 */
int parseLine(char* line){
        int i = strlen(line);
        while (*line < '0' || *line > '9') line++;
        line[i-3] = '\0';
        i = atoi(line);
	return i;
}

/* 
 *	Memory usage in KB 
 */
int memoryUsage(){
        FILE* file = fopen("/proc/self/status", "r");

        if(file==NULL)
                exit(1);

        int result = -1;
        char line[128];


        while (fgets(line, 128, file) != NULL){
            if (strncmp(line, "VmRSS:", 6) == 0){
                result = parseLine(line);
                break;
            }
        }
        fclose(file);
        return result;
}

static void grid_idle(client_t client);

/*
 *	Free workunit
 */
static void free_workunit(workunit_t workunit){
	xbt_free(workunit->number);
	xbt_free(workunit->times);	
	xbt_free(workunit->input_files);
	xbt_free(workunit);
}

/* 
 *	Free task 
 */
static void free_task(task_t task)
{
	if (task->project->running_task == task) {
		task->running = 0;
		MSG_task_cancel(task->msg_task);
		task->project->running_task = NULL;
		grid_idle(task->project->client);
	}
	if (task->heap_index >= 0)
		xbt_heap_remove(task->project->client->deadline_missed, task->heap_index);

	if (xbt_swag_belongs(task, task->project->run_list))
		xbt_swag_remove(task, task->project->run_list);

	if (xbt_swag_belongs(task, task->project->sim_tasks))
		xbt_swag_remove(task, task->project->sim_tasks);

	if (xbt_swag_belongs(task, task->project->tasks))
		xbt_swag_remove(task, task->project->tasks);

	MSG_task_destroy(task->msg_task);
	xbt_free(task->name);
	xbt_free(task);
}

/* 
 *	Free project 
 */
static void free_project(project_t proj)
{
	task_t task = NULL;
	
	xbt_swag_foreach(task, proj->tasks) {
		if(task->name)
			xbt_free(task->name);
		if(task->msg_task)
			MSG_task_destroy(task->msg_task);
	}

	xbt_free(proj->name);
	xbt_free(proj->answer_mailbox);
	xbt_queue_free(&(proj->tasks_ready));
	xbt_queue_free(&(proj->number_executed_task));
	xbt_queue_free(&(proj->workunit_executed_task));
	xbt_swag_free(proj->tasks);
	xbt_swag_free(proj->sim_tasks);
	xbt_swag_free(proj->run_list);
	xbt_free(proj);
}

/* 
 *	Free clients 
 */
static void free_client(client_t client)
{
	xbt_dict_free(&(client->projects));
	xbt_heap_free(client->deadline_missed);
	xbt_mutex_destroy(client->sched_mutex);
	xbt_cond_destroy(client->sched_cond);
	xbt_mutex_destroy(client->work_fetch_mutex);
	xbt_cond_destroy(client->work_fetch_cond);
	xbt_mutex_destroy(client->ask_for_work_mutex);
	xbt_cond_destroy(client->ask_for_work_cond);
	xbt_mutex_destroy(client->mutex_init);
	xbt_cond_destroy(client->cond_init);
	xbt_free(client);
}

static void grid_online(client_t client)
{
	double power;
	int32_t time;

	power = client->speed/1000000000.0;
	time = (int32_t)MSG_get_clock();

	xbt_mutex_acquire(_grid_power_mutex);
	if(client->stats_online){
		xbt_mutex_release(_grid_power_mutex);
		return;
	}
	_grid_online_power_deltas[time] += power;
	if(!client->running_project || !client->running_project->running_task){
		_grid_idle_power_deltas[time] += power;
		client->stats_idle = 1;
	}
	client->stats_online = 1;
	xbt_mutex_release(_grid_power_mutex);
}

static void grid_offline(client_t client)
{
	double power;
	int32_t time;

	power = client->speed/1000000000.0;
	time = (int32_t)MSG_get_clock();

	xbt_mutex_acquire(_grid_power_mutex);
	if(!client->stats_online){
		xbt_mutex_release(_grid_power_mutex);
		return;
	}
	_grid_online_power_deltas[time] -= power;
	if(client->stats_idle){
		_grid_idle_power_deltas[time] -= power;
		client->stats_idle = 0;
	}
	client->stats_online = 0;
	xbt_mutex_release(_grid_power_mutex);
}

static void grid_busy(client_t client)
{
	double power;
	int32_t time;

	power = client->speed/1000000000.0;
	time = (int32_t)MSG_get_clock();

	xbt_mutex_acquire(_grid_power_mutex);
	if(!client->stats_online || !client->stats_idle){
		xbt_mutex_release(_grid_power_mutex);
		return;
	}
	_grid_idle_power_deltas[time] -= power;
	client->stats_idle = 0;
	xbt_mutex_release(_grid_power_mutex);
}

static void grid_idle(client_t client)
{
	double power;
	int32_t time;

	power = client->speed/1000000000.0;
	time = (int32_t)MSG_get_clock();

	xbt_mutex_acquire(_grid_power_mutex);
	if(!client->stats_online || client->stats_idle){
		xbt_mutex_release(_grid_power_mutex);
		return;
	}
	_grid_idle_power_deltas[time] += power;
	client->stats_idle = 1;
	xbt_mutex_release(_grid_power_mutex);
}

static double distribution_mean(char num, double a, double b)
{
	switch((int)num){
		case 0:
			if (a <= 0)
				return 0;
			return b * tgamma(1.0 + 1.0 / a);
		case 1:
			return a * b;
		case 2:
			return exp(a + b * b / 2.0);
		case 3:
			return a;
		case 4:
			return a;
		case 5:
			if (a <= 0)
				return 0;
			return 1.0 / a;
		case 6:
			return 1;
		case 7:
			return 0;
		default:
			return 0;
	}
}

static double clamped_distribution_mean(char num, double a, double b, double min_speed, double max_speed)
{
	double mean = distribution_mean(num, a, b);

	if (max_speed < min_speed) {
		double aux = min_speed;
		min_speed = max_speed;
		max_speed = aux;
	}

	if ((int)num == 5 && a > 0) {
		double lower = max(min_speed, 0.0);
		double upper = max(max_speed, lower);
		double p_low = 1.0 - exp(-a * lower);
		double p_high = exp(-a * upper);
		double middle = (lower + 1.0 / a) * exp(-a * lower) - (upper + 1.0 / a) * exp(-a * upper);
		return lower * p_low + middle + upper * p_high;
	}

	if (mean < min_speed)
		return min_speed;
	if (mean > max_speed)
		return max_speed;
	return mean;
}

static double group_availability_mean(group_t group)
{
	double online = max(distribution_mean(group->av_distri, group->aa_param, group->ab_param), 0.0);
	double offline = max(distribution_mean(group->nv_distri, group->na_param, group->nb_param), 0.0);
	double total = online + offline;

	if (total <= 0)
		return 0;

	return online / total;
}

static void wait_groups_initialized(void)
{
	int32_t i;

	for (i = 0; i < NUMBER_CLIENT_GROUPS; i++) {
		xbt_mutex_acquire(_group_info[i].mutex);
		while (_group_info[i].on == 0)
			xbt_cond_wait(_group_info[i].cond, _group_info[i].mutex);
		xbt_mutex_release(_group_info[i].mutex);
	}
}

static void initialize_tail_budget(pdatabase_t database)
{
	int32_t i;
	double total_percentage = 0;
	double project_ratio = 1.0;

	if (database->tail_budget_initialized || !database->activate_tail_stage)
		return;

	wait_groups_initialized();

	database->theoretical_flops_budget = 0;
	for (i = 0; i < NUMBER_CLIENT_GROUPS; i++) {
		double mean_speed = _group_info[i].tail_mean_speed >= 0 ? _group_info[i].tail_mean_speed : clamped_distribution_mean(_group_info[i].sp_distri, _group_info[i].sa_param, _group_info[i].sb_param, _group_info[i].min_speed, _group_info[i].max_speed);
		double availability = _group_info[i].tail_availability >= 0 ? _group_info[i].tail_availability / 100.0 : group_availability_mean(&_group_info[i]);
		if (availability > 1.0)
			availability = 1.0;
		database->theoretical_flops_budget += _group_info[i].n_clients * mean_speed * 1000000000.0 * availability * sim_duration;
	}

	if (_num_clients_t > 0)
		project_ratio = min((double)database->nclients / (double)_num_clients_t, 1.0);
	database->theoretical_flops_budget *= project_ratio;
	database->effective_flops_budget = max(database->theoretical_flops_budget * database->utilization_safety, 0.0);

	for (i = 0; i < database->applications_num; i++) {
		total_percentage += max(database->applications[i].percentage, 0.0);
	}

	for (i = 0; i < database->applications_num; i++) {
		double app_budget = 0;
		double workunit_cost = (double)database->applications[i].target_nresults * (double)database->applications[i].job_duration;
		if (total_percentage > 0)
			app_budget = database->effective_flops_budget * max(database->applications[i].percentage, 0.0) / total_percentage;
		if (workunit_cost > 0)
			database->applications[i].tail_target_workunits_total = (int64_t)floor(app_budget / workunit_cost);
		else
			database->applications[i].tail_target_workunits_total = 0;
		database->applications[i].workunits_number = database->applications[i].tail_target_workunits_total;
		database->applications[i].sleep_time = (int64_t)ceil(sim_duration);
	}

	database->tail_budget_initialized = 1;
}

/* 
 *	Task update index 
 */
static void task_update_index(void *task, int index)
{
	//printf(":::::::::::::::::::::::::::::     %d\n", index);
	((task_t)task)->heap_index = index;
}

/* 
 *	Disk access simulation 
 */
void disk_access(int32_t server_number, int64_t size)
{
	pdatabase_t database = &_pdatabase[server_number];		// Server info

	// Calculate sleep time
	double sleep = min((double)sim_duration-MSG_get_clock()-PRECISION, (double)size/database->disk_bw);
	if(sleep < 0) sleep = 0;
	
	// Sleep
	MSG_process_sleep(sleep);
}

/*
 *	Process has done i out of n rounds,
 *	and we want a bar of width w and resolution r.
 */
static inline void loadBar(int x, int n, int r, int w)
{
	// Only update r times.
	if ( x % (n/r +1) != 0 ) return;
 
    	// Calculuate the ratio of complete-to-incomplete.
    	float ratio = x/(float)n;
    	int   c     = ratio * w;
 
	// Show the percentage complete.
	printf("Progress: %3d%% [", (int)(ratio*100) );
 
    	// Show the load bar.
    	for (x=0; x<c; x++)
       		printf("=");
 
    	for (x=c; x<w; x++)
       		printf(" ");
 
    	// ANSI Control codes to go back to the
    	// previous line and clear it.
    	printf("]\n\033[F\033[J");
}
	
/*
 *	 Server compute simulation
 */
void compute_server(int flops){
	msg_task_t task = NULL;
	task = MSG_task_create("compute_server", flops, 0, NULL);
	MSG_task_execute(task);
	MSG_task_destroy(task);
}

/*
 *	Print server results 
 */
int print_results(){
	int memory = 0;			// memory usage
	int memoryAux;			// memory aux
	int progress;			// Progress [0, 100]
	int64_t i, j, k, l;		// Indices	
	double sleep;			// Sleep time	
	double online_power = 0;
	double idle_power = 0;
	double busy_power;
	double grid_utilization;
	pdatabase_t database = NULL;	// Server info pointer

	// Init variables
	k = l = 0;
	sleep = sim_duration/100.0;			// 1 hour

	// Print progress
	for(progress=0; ceil(MSG_get_clock()) < sim_duration;){
		progress =(int)round(MSG_get_clock()/sim_duration*100) + 1;
		loadBar((int)round(progress), 100, 200, 50);
		memoryAux = memoryUsage();
		if(memoryAux > memory)
			memory = memoryAux;
		MSG_process_sleep(sleep);			// Sleep while simulation
	}

	setlocale(LC_NUMERIC, "en_US.UTF-8");

	printf("\n Memory usage: %'d KB\n", memory);

	printf("\n Total number of clients: %'d\n\n", _num_clients_t);

	// Iterate servers information
	for(i=0; i<NUMBER_PROJECTS; i++){
		database = &_pdatabase[i];	// Server info pointer
		
		// Print results
		printf("\n ####################  %s  ####################\n", database->project_name);
		printf("\n Simulation ends in %'g h (%'g sec)\n\n", MSG_get_clock()/3600.0, MSG_get_clock());
		for(j=0; j<(int64_t)database->nscheduling_servers; j++, l++) printf(" Scheduling server %" PRId64 ":\tBusy: %0.1f%%\n", j, _sserver_info[l].time_busy/sim_duration*100);
		for(j=0; j<(int64_t)database->ndata_servers; j++, k++) printf(" Data server %" PRId64 ":\t\tBusy: %0.1f%%\n", j, _dserver_info[k].time_busy/sim_duration*100);
		printf("\n  Number of clients: %'d\n", database->nclients);
		printf("  Tail stage active: \t\t%d\n", database->activate_tail_stage);
		printf("  Utilization safety: \t\t%0.4f\n", database->utilization_safety);
		printf("  Theoretical flops budget: \t%0.0f\n", database->theoretical_flops_budget);
		printf("  Effective flops budget: \t%0.0f\n", database->effective_flops_budget);
		printf("  Messages received: \t\t%'" PRId64 " (work requests received + results received)\n", database->nmessages_received);
		printf("  Work requests received: \t%'" PRId64 "\n", database->nwork_requests);
		printf("  Results created: \t\t%'" PRId64 " (%0.1f%%)\n", database->nresults, (double)database->nresults/database->nwork_requests*100);
		printf("  Results sent: \t\t%'" PRId64 " (%0.1f%%)\n", database->nresults_sent, (double)database->nresults_sent/database->nresults*100);	
		printf("  Results received: \t\t%'" PRId64 " (%0.1f%%)\n", database->nresults_received, (double)database->nresults_received/database->nresults*100);
		printf("  Results analyzed: \t\t%'" PRId64 " (%0.1f%%)\n", database->nresults_analyzed, (double)database->nresults_analyzed/database->nresults_received*100);
		printf("  Results success: \t\t%'" PRId64 " (%0.1f%%)\n", database->nsuccess_results, (double)database->nsuccess_results/database->nresults_analyzed*100);
		printf("  Results failed: \t\t%'" PRId64 " (%0.1f%%)\n", database->nerror_results, (double)database->nerror_results/database->nresults_analyzed*100);
		printf("  Results too late: \t\t%'" PRId64 " (%0.1f%%)\n", database->ndelay_results, (double)database->ndelay_results/database->nresults_analyzed*100);
		printf("  Results valid: \t\t%'" PRId64 " (%0.1f%%)\n", database->nvalid_results, (double)database->nvalid_results/database->nresults_analyzed*100);
		printf("  Workunits total: \t\t%'" PRId64 "\n", database->nworkunits);
		printf("  Workunits completed: \t\t%'" PRId64 " (%0.1f%%)\n", database->nvalid_workunits+database->nerror_workunits, (double)(database->nvalid_workunits+database->nerror_workunits)/database->nworkunits*100);
		printf("  Workunits not completed: \t%'" PRId64 " (%0.1f%%)\n", (database->nworkunits-database->nvalid_workunits-database->nerror_workunits), (double)(database->nworkunits-database->nvalid_workunits-database->nerror_workunits)/database->nworkunits*100);
		printf("  Workunits valid: \t\t%'" PRId64 " (%0.1f%%)\n", database->nvalid_workunits, (double)database->nvalid_workunits/database->nworkunits*100);
		printf("  Workunits valid but not completed: \t\t%'" PRId64 " (%0.1f%%)\n", database->nvalid_workunits_not_finished, (double)database->nvalid_workunits_not_finished/database->nworkunits*100);
		printf("  Workunits error: \t\t%'" PRId64 " (%0.1f%%)\n", database->nerror_workunits, (double)database->nerror_workunits/database->nworkunits*100);	
		printf("  Throughput: \t\t\t%'0.1f mens/s\n", (double)database->nmessages_received/sim_duration);
		printf("  Credit granted: \t\t%'" PRId64 " credits\n\n\n", (long int)database->total_credit);
		FILE *task_dynamic_file = fopen("../exp/task_dynamic", "w+");
		FILE *task_creation_file = fopen("../exp/workunits_creation", "w+");
		FILE *clients_dynamic_file = fopen("../exp/clients_dynamic", "w+");
		for (j = 0; j < database->applications_num; j++) {
			printf("Application %ld\n", j);
			application_t application = &database->applications[j];
			printf("  Workunits total: \t\t%'" PRId64 "\n", application->nworkunits);
			printf("  Tail target workunits total: \t%'" PRId64 "\n", application->tail_target_workunits_total);
			printf("  Workunit cost in flops: \t%0.0f\n", (double)application->target_nresults * (double)application->job_duration);
			for(k=0; k<sim_duration; k++) fprintf(task_dynamic_file, "%ld %d\n", j,  application->valid_workunits_timestamps[k]);	
			for(k=0; k<sim_duration; k++) fprintf(task_creation_file, "%ld %d\n", j,  application->creation_workunit_timestamps[k]);	
			printf("\n");
		}
		fclose(task_dynamic_file);
		fclose(task_creation_file);
		for(j=0; j<sim_duration; j++) fprintf(clients_dynamic_file, "%d\n", database->clients_availability[j]);
		fclose(clients_dynamic_file);
		//printf("  FLOPS average: \t\t%'" PRId64 " GFLOPS\n\n", (int64_t)((double)database->nvalid_results*(double)database->job_duration/sim_duration/1000000000.0));	
		FILE *task_completed_dynamic_file = fopen("../exp/task_dynamic_completed", "a+");
		for(j=0; j<sim_duration; j++) fprintf(task_completed_dynamic_file, "%d %d\n", database->applications[0].target_nresults, database->valid_completed_workunits_timestamps[j]);	
		fclose(task_completed_dynamic_file);
		FILE *workunits_all_dynamic_file = fopen("../exp/workunits_all_dynamic", "w+");
		for(j=0; j<sim_duration; j++) fprintf(workunits_all_dynamic_file, "%d\n",database->workunit_timestamps[j]);	
		fclose(workunits_all_dynamic_file);
		printf("written\n");
		
	}

	FILE *grid_utilization_file = fopen("../exp/grid_utilization", "w+");
	for(j=0; j<sim_duration; j++){
		online_power += _grid_online_power_deltas[j];
		idle_power += _grid_idle_power_deltas[j];
		busy_power = online_power - idle_power;
		if(online_power > 0)
			grid_utilization = busy_power / online_power;
		else
			grid_utilization = 0;
		fprintf(grid_utilization_file, "%0.6f\n", grid_utilization);
	}
	fclose(grid_utilization_file);

	return 0;
}

/*
 *	Init database
 */
int init_database(int argc, char *argv[])
{
	int i, project_number;
	pdatabase_t database;
	int j = 1;
	
	/*if (argc != 19) {
		printf("Invalid number of parameter in init_database()\n");
		return 0;
	}*/		

	project_number = atoi(argv[j++]);
	database = &_pdatabase[project_number];	

	// Init database
	database->project_number = project_number;			// Project number
	database->project_name = xbt_strdup(argv[j++]);			// Project name
	database->disk_bw = (int64_t)atoll(argv[j++]);			// Disk bandwidth
	database->ndata_servers = (char)atoi(argv[j++]);			// Number of data servers
	database->replication = (int32_t)atoi(argv[j++]);		// Input file replication
	database->activate_tail_stage = (int32_t)atoi(argv[j++]);
	database->utilization_safety = atof(argv[j++]);
	database->theoretical_flops_budget = 0;
	database->effective_flops_budget = 0;
	database->tail_budget_initialized = 0;
	database->applications_num = (int32_t)atoi(argv[j++]);
	database->nmessages_received = 0;				// Store number of messages rec.
	database->nresults = 0;						// Number of results created
	database->nresults_sent = 0;					// Number of results sent
	database->nwork_requests = 0;					// Store number of requests rec.
	database->nvalid_results = 0;					// Number of valid results (with a consensus)
	database->nresults_received = 0;				// Number of results received (replies)
	database->nresults_analyzed = 0;				// Number of results analyzed
	database->nsuccess_results = 0;					// Number of success results
	database->nerror_results = 0;					// Number of erroneous results
	database->ndelay_results = 0;					// Number of delayed results
	database->total_credit = 0;					// Total credit granted
	database->nworkunits = 0;					// Number of workunits created
	database->nvalid_workunits = 0;					// Number of valid workunits
	database->nvalid_workunits_not_finished = 0;
	database->nerror_workunits = 0;					// Number of erroneous workunits
	database->nfinished_scheduling_servers = 0;			// Number of finished scheduling servers

	database->applications = xbt_new(struct application, database->applications_num);
	for (i = 0; i < database->applications_num; i++) {
		database->applications[i].percentage = (double)atof(argv[j++]);	
		database->applications[i].output_file_size = (int64_t)atoll(argv[j++]);		// Answer size
		database->applications[i].job_duration = (int64_t) atoll(argv[j++]);		// Workunit duration
		database->applications[i].ifgl_percentage = (char)atoi(argv[j++]); 		// Percentage of input files generated locally
		database->applications[i].ifcd_percentage = (char)atoi(argv[j++]);			// Number of workunits that share the same input files
		database->applications[i].min_quorum = (int32_t)atoi(argv[j++]);			// Quorum
		database->applications[i].target_nresults = (int32_t)atoi(argv[j++]);		// target_nresults
		database->applications[i].max_error_results = (int32_t)atoi(argv[j++]);		// max_error_results
		database->applications[i].max_total_results = (int32_t)atoi(argv[j++]);		// Maximum number of times a task must be sent
		database->applications[i].max_success_results = (int32_t)atoi(argv[j++]);		// max_success_results
		database->applications[i].delay_bound = (int64_t)atoll(argv[j++]);		// Workunit deadline
		database->applications[i].success_percentage = (char)atoi(argv[j++]);		// Success results percentage
		database->applications[i].canonical_percentage = (char)atoi(argv[j++]);		// Canonical results percentage
		database->applications[i].input_file_size = (int64_t)atoll(argv[j++]);		// Input file size
		database->applications[i].workunits_number = (int64_t)atoll(argv[j++]);
		database->applications[i].sleep_time = (int64_t)atoll(argv[j++]);

		database->applications[i].nworkunits = 0;		// Number of workunits created
		database->applications[i].nvalid_workunits = 0;	// Number of workunits validated
		database->applications[i].nvalid_workunits_not_finished = 0;
		database->applications[i].nerror_workunits = 0;	// Number of erroneous workunits
		database->applications[i].valid_workunits_timestamps = xbt_new0(int32_t, 10000000);;
		database->applications[i].valid_completed_workunits_timestamps = xbt_new0(int32_t, 10000000);;
		database->applications[i].creation_workunit_timestamps = xbt_new0(int32_t, 10000000);

		database->applications[i].tail_target_workunits_total = 0;
		database->applications[i].is_on = 1;
		database->applications[i].suspended_until = 0;
		database->applications[i].nworkunits_cur = 0;
	} 
	printf("initialized\n");
	
	// Fill with data server names
	database->data_servers = xbt_new0(char*, (int) database->ndata_servers);
	database->valid_workunits_timestamps = xbt_new0(int32_t, 10000000);
	database->valid_completed_workunits_timestamps  = xbt_new0(int32_t, 10000000);
	database->workunit_timestamps = xbt_new0(int32_t, 10000000);
	database->clients_availability = xbt_new0(int32_t, 10000000);
	for(i=0; i<database->ndata_servers; i++)
		database->data_servers[i] = bprintf("d%" PRId32 "%" PRId32, project_number+1, i);

	MSG_barrier_wait(database->barrier);	

	return 0;
}

/*
 *	Generate workunit
 */
workunit_t generate_workunit(pdatabase_t database){
	int i;
	workunit_t workunit = xbt_new(s_workunit_t, 1);	
	double current = 0;
	workunit->number = bprintf("%" PRId64, database->nworkunits);	
	workunit->status = IN_PROGRESS;
	workunit->ntotal_results = 0;
	workunit->nsent_results = 0;
	workunit->nresults_received = 0;
	workunit->nvalid_results = 0;
	workunit->nsuccess_results = 0;
	workunit->nerror_results = 0;
	workunit->ncurrent_error_results = 0;
	workunit->credits = -1;
	double sum = 0;
	for (i = 0; i < database->applications_num; i++) {
		if (!database->applications[i].is_on) {
			continue;
		}
		sum += database->applications[i].percentage;
	}
	double rand = uniform_ab(0, sum);
	for (i = 0; i < database->applications_num; i++) {
		if (!database->applications[i].is_on) {
			continue;
		}
		current += database->applications[i].percentage;
		if (rand < current) {
			workunit->application = i;
			break;
		}
	}
	workunit->times = xbt_new(double, database->applications[workunit->application].max_total_results);
	workunit->ninput_files = database->replication;
	workunit->input_files=xbt_new(char *, workunit->ninput_files);

	for(i=0; i<workunit->ninput_files; i++)
		workunit->input_files[i] = database->data_servers[uniform_int(0, database->ndata_servers-1)];
	database->nworkunits++;
	database->applications[workunit->application].nworkunits++;
	database->applications[workunit->application].nworkunits_cur++;
	database->applications[workunit->application].creation_workunit_timestamps[(int)MSG_get_clock()] += 1;

	return workunit;
}

/*
 *	Generate result
 */
result_t generate_result(pdatabase_t database, workunit_t workunit, int X){
	result_t result = xbt_new(s_result_t, 1);
	result->workunit = workunit;
	result->ninput_files = workunit->ninput_files;
	result->input_files = workunit->input_files;	
	database->ncurrent_results++;	
	database->nresults++;

	workunit->times[(int)workunit->ntotal_results++]=MSG_get_clock();

	if(X == 1)
		workunit->ncurrent_error_results--;

	if(database->ncurrent_results >= 1)
		xbt_cond_signal(database->wg_empty);

	return result;
}

/*
 *	Work generator
 */
int work_generator(int argc, char *argv[])
{
	int project_number, i;
	pdatabase_t database;

	if (argc != 2) {
		printf("Invalid number of parameter in work_generator()\n");
		return 0;
	}		

	project_number = atoi(argv[1]);
	database = &_pdatabase[project_number];	

	// Wait until the database is initiated
	MSG_barrier_wait(database->barrier);

	if (database->activate_tail_stage)
		initialize_tail_budget(database);

	while(!database->wg_end){
		
		xbt_mutex_acquire(database->r_mutex);
	
		while(database->ncurrent_results >= MAX_BUFFER && !database->wg_end) {
			xbt_cond_wait(database->wg_full, database->r_mutex);	
		}
		if(database->wg_end){
			xbt_mutex_release(database->r_mutex);
			break;
		}

		workunit_t workunit = NULL;

		// Check if there are error results
		xbt_mutex_acquire(database->er_mutex);
		if(database->ncurrent_error_results > 0){
			while(database->ncurrent_error_results > 0){
				// Get workunit associated with the error result
				xbt_queue_pop(database->current_error_results, (char *)&workunit);
				database->ncurrent_error_results--;
				xbt_mutex_release(database->er_mutex);
			
				// Generate new instance from the workunit	
				result_t result = generate_result(database, workunit, 1);
				xbt_queue_push(database->current_results, (const char *)&(result));	
			}	
			xbt_mutex_acquire(database->er_mutex);		
		}
		// Create new workunit and target_nresults
		else {
			int32_t has_active_app = 0;
			int32_t has_future_app = 0;
			double first_active = 0;
			for (i = 0; i < database->applications_num; i++) {
				application_t application = &database->applications[i];
				if (application->is_on) {
					if (application->nworkunits_cur == application->workunits_number) {
						printf("application %d is sleeping application->nworkunits_cur %d\n", i, application->nworkunits_cur);
						application->is_on = 0;
						application->suspended_until = min(MSG_get_clock() + application->sleep_time, sim_duration);
						application->nworkunits_cur = 0;
					}
				} else {
					if (application->suspended_until < MSG_get_clock()) {
						application->is_on = 1;
					}
				}
				if (application->is_on) {
					has_active_app = 1;
					has_future_app = 1;
				} else if (application->suspended_until > MSG_get_clock()) {
					has_future_app = 1;
					if (first_active == 0)
						first_active = application->suspended_until;
					else
						first_active = min(first_active, application->suspended_until);
				}
			}
			if (has_active_app){
				workunit_t workunit = generate_workunit(database);
				xbt_dict_set(database->current_workunits, workunit->number, workunit, (void_f_pvoid_t) free_workunit); 		

				for(i=0; i<database->applications[workunit->application].target_nresults; i++){
					result_t result = generate_result(database, workunit, 0);
					xbt_queue_push(database->current_results, (const char *)&(result));	
				}
			} else if (has_future_app) {
				xbt_mutex_release(database->r_mutex);
				xbt_ex_t e;
				TRY {
					while (database->ncurrent_error_results == 0 && !database->wg_end) {
						xbt_cond_timedwait(database->wg_err, database->er_mutex, first_active - MSG_get_clock());
					}
					xbt_mutex_release(database->er_mutex);
				} CATCH(e) {
					xbt_ex_free(e);
				}
				continue;
			} else {
				xbt_mutex_release(database->r_mutex);
				while (database->ncurrent_error_results == 0 && !database->wg_end) {
					xbt_cond_wait(database->wg_err, database->er_mutex);
				}
				xbt_mutex_release(database->er_mutex);
				continue;
			}
		}
		xbt_mutex_release(database->er_mutex);
		xbt_mutex_release(database->r_mutex);			
	}
	
	return 0;
}

/*
 *	Validator
 */
int validator(int argc, char *argv[])
{
	int project_number;
	workunit_t workunit;
	pdatabase_t database;
	reply_t reply = NULL;

	if (argc != 2) {
		printf("Invalid number of parameter in validator()\n");
		return 0;
	}		

	project_number = atoi(argv[1]);
	database = &_pdatabase[project_number];	

	// Wait until the database is initiated
	MSG_barrier_wait(database->barrier);
	
	while(!database->v_end){
		
		xbt_mutex_acquire(database->v_mutex);
	
		while(database->ncurrent_validations == 0 && !database->v_end)
			xbt_cond_wait(database->v_empty, database->v_mutex);		

		if(database->v_end){
			xbt_mutex_release(database->v_mutex);
			break;
		}
		
		// Get received result
		xbt_queue_pop(database->current_validations, (char *)&reply);
		database->ncurrent_validations--;
		xbt_mutex_release(database->v_mutex);
		//printf("validating\n");

		// Get asociated workunit
		workunit = xbt_dict_get(database->current_workunits, reply->workunit);
		workunit->nresults_received++;

		// Delay result
		if(MSG_get_clock()-workunit->times[reply->result_number] >= database->applications[workunit->application].delay_bound){
			//printf("delayed\n");
			reply->status = FAIL;
			workunit->nerror_results++;
			database->ndelay_results++;
		}
		// Success result
		else if(reply->status == SUCCESS){
			//printf("success\n");
			workunit->nsuccess_results++;
			database->nsuccess_results++;
			if(reply->value == CORRECT){
				workunit->nvalid_results++;
				if(workunit->credits == -1) workunit->credits = reply->credits;
				else workunit->credits = workunit->credits > reply->credits ? reply->credits : workunit->credits;
			}
		}
		// Error result
		else{
			//printf("error\n");
			workunit->nerror_results++;
			database->nerror_results++;
		}
		database->nresults_analyzed++;
		FILE* got_results = fopen("../exp/got_results", "a+");
		fprintf(got_results, "%d %d\n", reply->value == CORRECT, (int)MSG_get_clock());
		fclose(got_results);
	
		// Check workunit
		xbt_mutex_acquire(database->er_mutex);
		if(workunit->status == IN_PROGRESS){
			if(workunit->nvalid_results 			>= 	database->applications[workunit->application].min_quorum){ 
				workunit->status = VALID;
				database->nvalid_workunits_not_finished++;
				database->nvalid_results += (int64_t)(workunit->nvalid_results);
				database->total_credit += (int64_t)(workunit->credits*workunit->nvalid_results);	
				database->valid_workunits_timestamps[(int32_t)MSG_get_clock()] += 1;
				database->applications[workunit->application].valid_workunits_timestamps[(int32_t)MSG_get_clock()] += 1;
			}
			else if(workunit->ntotal_results 		>=	database->applications[workunit->application].max_total_results		||
				workunit->nerror_results 		>= 	database->applications[workunit->application].max_error_results 		||
				workunit->nsuccess_results 		>=	database->applications[workunit->application].max_success_results 
				) workunit->status = ERROR;
		}
		else if(workunit->status == VALID && reply->status == SUCCESS && reply->value == CORRECT){
			database->nvalid_results++;
			database->total_credit += (int64_t)(workunit->credits);
		}
	
		// If result is an error and task is not completed, call work generator in order to create a new instance
		if(reply->status == FAIL){	
			if(	workunit->status 			==	IN_PROGRESS				&&
				workunit->nsuccess_results		<	database->applications[workunit->application].max_success_results 		&&
				workunit->nerror_results		<	database->applications[workunit->application].max_error_results 		&&
				workunit->ntotal_results		<	database->applications[workunit->application].max_total_results)
			{	
				xbt_queue_push(database->current_error_results, (const char *)&(workunit));	
				database->ncurrent_error_results++;
				xbt_cond_signal(database->wg_err);
				workunit->ncurrent_error_results++;	
			}
		}

		// Call asimilator if workunit has been completed	
		if(	(workunit->status 			!= 	IN_PROGRESS) 			&& 
			(workunit->nresults_received		==	workunit->ntotal_results)	&&
			(workunit->ncurrent_error_results	==	0)					){
			xbt_mutex_acquire(database->a_mutex);	
			xbt_queue_push(database->current_assimilations, (const char *)&(workunit->number));
			database->ncurrent_assimilations++;
			database->valid_completed_workunits_timestamps[(int32_t)MSG_get_clock()] += 1;
			xbt_cond_signal(database->a_empty);
			xbt_mutex_release(database->a_mutex);
		}
		xbt_mutex_release(database->er_mutex);

		xbt_free(reply);
		reply = NULL;		
	}
	
	return 0;	
}

/*
 *	File deleter
 */
int file_deleter(pdatabase_t database, char* workunit_number){
	xbt_dict_remove(database->current_workunits, workunit_number);
	return 0;
}

/*
 *	Assimilator
 */
int assimilator(int argc, char *argv[])
{
	int project_number;
	pdatabase_t database;
	workunit_t workunit;
	char *workunit_number;

	if (argc != 2) {
		printf("Invalid number of parameter in assimilator()\n");
		return 0;
	}		

	project_number = atoi(argv[1]);
	database = &_pdatabase[project_number];	

	// Wait until the database is initiated
	MSG_barrier_wait(database->barrier);	

	while(!database->a_end){
		
		xbt_mutex_acquire(database->a_mutex);
	
		while(database->ncurrent_assimilations == 0 && !database->a_end)
			xbt_cond_wait(database->a_empty, database->a_mutex);		

		if(database->a_end){
			xbt_mutex_release(database->a_mutex);
			break;
		}

		// Get workunit number to assimilate
		xbt_queue_pop(database->current_assimilations, (char *)&workunit_number);
		database->ncurrent_assimilations--;		
		xbt_mutex_release(database->a_mutex);

		// Get completed workunit
		workunit = xbt_dict_get(database->current_workunits, workunit_number);

		// Update workunit stats
		if(workunit->status == VALID){
			database->nvalid_workunits++;
		}
		else
		{
			database->nerror_workunits++;	
		}
		// Delete completed workunit from database
		file_deleter(database, workunit->number);	
	}
	
	return 0;	
}

/*
 *	Select result from database
 */
result_t select_result(int project_number, request_t req){
	//printf("select result\n");
	task_t task = NULL;
	pdatabase_t database = NULL;
	result_t result = NULL;
	int i;

	database = &_pdatabase[project_number];
	// Get result
	xbt_queue_pop(database->current_results, (char *)&result);
		
	// Signal work generator if number of current results is 0 
	database->ncurrent_results--;
	if(database->ncurrent_results == 0)
		xbt_cond_signal(database->wg_full);

	double job_duration = database->applications[result->workunit->application].job_duration;

	// Calculate number of tasks
	result->number_tasks = (int32_t) floor(req->percentage/(job_duration/req->speed));
	if (result->number_tasks == 0) result->number_tasks = (int32_t) 1;
		
	// Create tasks
	result->tasks = xbt_new0(task_t, (int) result->number_tasks);
	
	// Fill tasks
	for (i = 0; i < result->number_tasks; i++) {
		task = xbt_new0(s_task_t, 1);
		task->workunit = result->workunit->number;
		task->application = result->workunit->application;
		task->name = bprintf("%" PRId32, result->workunit->nsent_results++);
 		task->duration = job_duration*((double)req->group_speed/req->speed);	
		task->deadline = database->applications[result->workunit->application].delay_bound;
		task->start = MSG_get_clock();
		task->heap_index = -1;
		result->tasks[i] = task;
	}

	xbt_mutex_acquire(database->ssdmutex);
	database->nresults_sent++;
	xbt_mutex_release(database->ssdmutex);
	
	return result;
}

/* 
 *	Scheduling server requests function
 */
int scheduling_server_requests(int argc, char *argv[])
{
	msg_task_t task = NULL;					// Task
  	ssmessage_t msg = NULL;					// Client message
	pdatabase_t database = NULL;				// Database
	sserver_t sserver_info = NULL;				// Scheduling server info
	int32_t scheduling_server_number, project_number;	// Server number, index

	// Check number of arguments
	if (argc != 3) {
		printf("Invalid number of parameter in scheduling_server_requests()\n");
		return 0;
	}		

	// Init boinc server
	project_number = (int32_t)atoi(argv[1]);			// Project number
	scheduling_server_number = (int32_t)atoi(argv[2]);		// Scheduling server number	
	
	database = &_pdatabase[project_number];				// Database
	sserver_info = &_sserver_info[scheduling_server_number];	// Scheduling server info
	
	sserver_info->server_name = MSG_host_get_name(MSG_host_self());	// Server name
	
	// Wait until database is ready
	MSG_barrier_wait(database->barrier);
	
	/* 
		Set asynchronous mailbox mode in order to receive requests in spite of 
		the fact that the server is not calling MSG_task_receive()
	*/
	MSG_mailbox_set_async(sserver_info->server_name); 
	
	while (1) {
		
		// Receive message
		MSG_task_receive(&(task), sserver_info->server_name);		

		// Unpack message
		msg = (ssmessage_t)MSG_task_get_data(task);
		
		// Termination message
		if(msg->type == TERMINATION){
			MSG_task_destroy(task);
			xbt_free(msg);
			break;
		}
		// Client answer with execution results
		else if(msg->type == REPLY){
			xbt_mutex_acquire(database->ssrmutex);
			database->nmessages_received++;
			database->nresults_received++;
			xbt_mutex_release(database->ssrmutex);
		}
		// Client work request
		else{
			xbt_mutex_acquire(database->ssrmutex);
			database->nmessages_received++;
			database->nwork_requests++;
			xbt_mutex_release(database->ssrmutex);
		}

		// Insert request into queue
  	  	xbt_mutex_acquire(sserver_info->mutex);
  	  	sserver_info->Nqueue++;
		xbt_queue_push(sserver_info->client_requests, (const char *)&msg);
		
		// If queue is not empty, wake up dispatcher process	
		if (sserver_info->Nqueue > 0)
			xbt_cond_signal(sserver_info->cond);
  	  	xbt_mutex_release(sserver_info->mutex);

		// Freeprintf("%0.1f", random);
		MSG_task_destroy(task);
		task = NULL;			
		msg = NULL;
	}  

	// Terminate dispatcher execution
	xbt_mutex_acquire(sserver_info->mutex);
	sserver_info->EmptyQueue = 1;
	xbt_cond_signal(sserver_info->cond);
    	xbt_mutex_release(sserver_info->mutex);

	return 0;
}

/* 
 *	Scheduling server dispatcher function 
 */
int scheduling_server_dispatcher(int argc, char *argv[])
{
	msg_task_t ans_msg_task = NULL;		// Task that is going to be sent
	ssmessage_t msg = NULL;			// Client request
	dsmessage_t work = NULL;		// Termination message
	result_t result = NULL;			// Data server answer
	msg_comm_t comm = NULL;			// Asynchronous communication
	pdatabase_t database = NULL;		// Server info
	sserver_t sserver_info = NULL;		// Scheduling server info	
	int32_t i, project_number;		// Index, project number
	int32_t scheduling_server_number;	// Scheduling_server_number		
	double t0, t1;				// Time measure	

	// Check number of arguments
	if (argc != 3) {
		printf("Invalid number of parameter in scheduling_server_dispatcher()\n");
		return 0;
	}

	// Init boinc dispatcher
	t0 = t1 = 0.0;
	project_number = (int32_t)atoi(argv[1]);			// Project number	
	scheduling_server_number = (int32_t)atoi(argv[2]);		// Scheduling server number

	database = &_pdatabase[project_number];				// Server info
	sserver_info = &_sserver_info[scheduling_server_number];	// Scheduling server info
		
	while (1) {		
		xbt_mutex_acquire(sserver_info->mutex);

		// Wait until queue is not empty
		while ((sserver_info->Nqueue ==  0)   && (sserver_info->EmptyQueue == 0)) {
			xbt_cond_wait(sserver_info->cond, sserver_info->mutex);
		}

		// Exit the loop when boinc server indicates it
		if ((sserver_info->EmptyQueue == 1) && sserver_info->Nqueue == 0) {
			xbt_mutex_release(sserver_info->mutex);
			break;
		}

		// Iteration start time
		t0 = MSG_get_clock();

		// Simulate server computation
		compute_server(36000000);

		// Pop client message
               	xbt_queue_pop(sserver_info->client_requests, (char *)&msg);
		sserver_info->Nqueue--;
		xbt_mutex_release(sserver_info->mutex);
	
		// Check if message is an answer with the computation results
		if(msg-> type == REPLY){
			xbt_mutex_acquire(database->v_mutex);
			//printf("reply received for validating %ld\n", database->nresults_received);
	
			// Call validator
			xbt_queue_push(database->current_validations, (const char *)&(msg->content));
			database->ncurrent_validations++;
				
			xbt_cond_signal(database->v_empty);
			xbt_mutex_release(database->v_mutex);
		}
		// Message is an address request
		else{
			// Consumer
			xbt_mutex_acquire(database->r_mutex);
			xbt_ex_t e;
			//printf("%ld\n", database->ncurrent_results);
			if (database->ncurrent_results == 0) {
				TRY {
					xbt_cond_timedwait(database->wg_empty, database->r_mutex, 5);
				} CATCH(e) {
					xbt_ex_free(e);
				}
			}
			if (database->ncurrent_results == 0) {
				//printf("sending zero result\n");
				result = xbt_new0(s_result_t, 1);
				result->number_tasks = 0;
				result->ninput_files = 1;
			} else {
				// CONSUME
				result = select_result(project_number, (request_t) msg->content);		
				//printf("%d\n",result->number_tasks);
			}	
			xbt_mutex_release(database->r_mutex);
			//printf("sending %d %d\n", result->number_tasks, (int)MSG_get_clock());
			FILE* sent_results_files = fopen("../exp/sent_results", "a+");
			fprintf(sent_results_files, "%d %d\n", result->number_tasks, (int)MSG_get_clock());
			fclose(sent_results_files);

			// Create the task
			ans_msg_task = MSG_task_create("answer_work_fetch", 0, KB*result->ninput_files, result);
		
			// Answer the client
			comm = MSG_task_isend(ans_msg_task, ((request_t)msg->content)->answer_mailbox);

			// Store the asynchronous communication created in the dictionary	
			xbt_dict_set(_sscomm, ((request_t)msg->content)->answer_mailbox, comm, NULL);
			xbt_free(msg->content);
		}
	
		// Iteration end time	
		t1 = MSG_get_clock();
		
		// Accumulate total time server is busy
		if(t0 < sim_duration) sserver_info->time_busy+=(t1-t0);		
			
		// Free
		xbt_free(msg);
		msg = NULL;
		ans_msg_task = NULL;
		result = NULL;
	}

	// Wait until all scheduling servers finish
	xbt_mutex_acquire(database->ssdmutex);
	database->nfinished_scheduling_servers++;
	xbt_mutex_release(database->ssdmutex);

	// Check if it is the last scheduling server
	if(database->nfinished_scheduling_servers == database->nscheduling_servers){
		// Send termination message to data servers
		for(i=0; i<database->ndata_servers; i++){
			// Create termination message
			work = xbt_new0(s_dsmessage_t, 1);
	
			// Group speed = -1 indicates it is a termination message
			work->type = TERMINATION;

			// Create the task
			ans_msg_task = MSG_task_create("ask_work", 0, 0, work);

			// Send message
			MSG_task_send(ans_msg_task, database->data_servers[i]);	

			// Free data server name
			xbt_free(database->data_servers[i]);
		}
		// Free
		xbt_free(database->data_servers);
		xbt_free(database->valid_workunits_timestamps);
		xbt_free(database->valid_completed_workunits_timestamps);
		xbt_free(database->clients_availability);
	
		// Finish project back-end	
		database->wg_end = 1;	
		database->v_end = 1;
		database->a_end = 1;
		xbt_cond_signal(database->wg_err);
		xbt_cond_signal(database->wg_full);	
		xbt_cond_signal(database->v_empty);
		xbt_cond_signal(database->a_empty);
	}	

	return 0;
} 

/* 
 *	Data server requests function 
 */
int data_server_requests(int argc, char *argv[])
{
	dserver_t dserver_info = NULL;
	msg_task_t task = NULL;
  	dsmessage_t req = NULL;
	int32_t server_number;			

	// Check number of arguments
	if (argc != 2) {
		printf("Invalid number of parameter in data_server_requests()\n");
		return 0;
	}

	// Init parameters
	server_number = (int32_t)atoi(argv[1]);				// Data server number
	dserver_info = &_dserver_info[server_number];			// Data server info pointer
	dserver_info->server_name = MSG_host_get_name(MSG_host_self());	// Data server name

	// Set asynchronous receiving in mailbox
	MSG_mailbox_set_async(dserver_info->server_name);
	
	while (1) {
		// Receive message	
		MSG_task_receive(&(task), dserver_info->server_name);		
		req = (dsmessage_t)MSG_task_get_data(task);

		// Termination message
		if(req->type == TERMINATION){
			MSG_task_destroy(task);
			xbt_free(req);
			break;
		}

		// Insert request into queue
  	  	xbt_mutex_acquire(dserver_info->mutex);
  	  	dserver_info->Nqueue++;
		xbt_queue_push(dserver_info->client_requests, (const char *)&req);	
		
		// If queue is not empty, wake up dispatcher process
		if (dserver_info->Nqueue > 0 )
			xbt_cond_signal(dserver_info->cond);  // wake up dispatcher
  	  	xbt_mutex_release(dserver_info->mutex);

		// Free
		MSG_task_destroy(task);
		task = NULL;
		req = NULL;		
	}  

	// Terminate dispatcher execution
	xbt_mutex_acquire(dserver_info->mutex);
	dserver_info->EmptyQueue = 1;
	xbt_cond_signal(dserver_info->cond);
    	xbt_mutex_release(dserver_info->mutex);

	return 0;
}

/* 
 *	Data server dispatcher function
 */
int data_server_dispatcher(int argc, char *argv[])
{
	pdatabase_t database = NULL;
	dserver_t dserver_info = NULL;
	msg_task_t ans_msg_task = NULL;
	dsmessage_t req = NULL;
	msg_comm_t comm = NULL;		// Asynch communication	
	int32_t server_number, project_number;
	double t0, t1;	

	// Check number of arguments
	if (argc != 3) {
		printf("Invalid number of parameter in data_server_dispatcher()\n");
		return 0;
	}

	// Init data dispatcer
	server_number = (int32_t)atoi(argv[1]);	
	project_number = (int32_t)atoi(argv[2]);

	dserver_info = &_dserver_info[server_number];		// Data server info pointer
	database = &_pdatabase[project_number];			// Boinc server info pointer	

	while (1) {
		xbt_mutex_acquire(_dserver_info[server_number].mutex);

		// Wait while queue is not empty
		while ((dserver_info->Nqueue ==  0)   && (dserver_info->EmptyQueue == 0)) {
			xbt_cond_wait(dserver_info->cond, dserver_info->mutex);
		}

		// Exit the loop when boinc server indicates it
		if ((dserver_info->EmptyQueue == 1) && (dserver_info->Nqueue == 0)) {
			xbt_mutex_release(dserver_info->mutex);
			break;
		}

		// Iteration start time
		t0 = MSG_get_clock();		

		// Simulate server computation
		compute_server(20);	

		// Pop client message
               	xbt_queue_pop(dserver_info->client_requests, (char *)&req);
		dserver_info->Nqueue--;
		xbt_mutex_release(dserver_info->mutex);

		application_t application = &database->applications[req->application];
		// Reply with output file
		if(req->type == REPLY){
			disk_access(project_number, application->output_file_size);
		}
		// Input file request
		else{
			// Read tasks from disk
			disk_access(project_number, application->input_file_size);	

			// Create the message
			ans_msg_task = MSG_task_create("input_file_task", 0, application->input_file_size, NULL);
	
			// Answer the client
			comm = MSG_task_isend(ans_msg_task, req->answer_mailbox);		
		
			// Store the asynchronous communication created in the dictionary
			xbt_dict_set(_dscomm, req->answer_mailbox, comm, NULL);
	
			ans_msg_task = NULL;
		}

		// Iteration end time
		t1 = MSG_get_clock();
		
		// Accumulate total time server is busy
		if(t0 < sim_duration) dserver_info->time_busy += (t1-t0);

		// Free
		xbt_free(req);
		req = NULL;
	}

	return 0;
} 

/* 
 *	Projects initialization 
 */
static void client_initialize_projects(client_t client, int argc, char *argv[])
{
	xbt_dict_t dict;
	int number_proj;
	int i, index;

	dict = xbt_dict_new();	// Initialize a new dictionary

	number_proj = atoi(argv[0]);

	if (argc - 1 != number_proj * 3) {
		printf("Invalid number of parameters to client: %d. It should be %d\n", argc-1, number_proj*3);
		xbt_abort();
	}

	index = 1;
	for (i = 0; i < number_proj; i++) {
		project_t proj;
		s_task_t t;
		proj = xbt_new0(s_project_t, 1);
		proj->name = xbt_strdup(argv[index++]);
		proj->number = (char)atoi(argv[index++]);	
		proj->priority = (char)atoi(argv[index++]);		
		proj->on = 1;

		proj->answer_mailbox = xbt_new(char, 20);
		strcpy(proj->answer_mailbox, proj->name);
		strcat(proj->answer_mailbox, client->name);

		proj->tasks_ready = xbt_queue_new(0, sizeof(task_t));
		proj->number_executed_task = xbt_queue_new(0, sizeof(int32_t));	// Queue with task's numbers
		proj->workunit_executed_task = xbt_queue_new(0, sizeof(char *));	// Queue with task's sizes
		proj->tasks = xbt_swag_new(xbt_swag_offset(t, tasks_hookup));
		proj->sim_tasks = xbt_swag_new(xbt_swag_offset(t, sim_tasks_hookup));
		proj->run_list = xbt_swag_new(xbt_swag_offset(t, run_list_hookup));

		proj->total_tasks_checked = 0;
                proj->total_tasks_executed = 0;
                proj->total_tasks_received = 0;
                proj->total_tasks_missed = 0;

		proj->client = client;

		xbt_dict_set(dict, proj->name, proj, (void_f_pvoid_t)free_project);
	}
	client->projects = dict;
}

/*
 *	Client ask for work:
 *
 *	- Request work to scheduling_server
 *	- Download input files from data server
 *	- Send execution results to scheduling_server
 *	- Upload output files to data server
 */
static int client_ask_for_work(client_t client, project_t proj, double percentage)
{
	/*

	WORK REQUEST NEEDS:
		
		- type: REQUEST
		- content: request_t
		- content->answer_mailbox: Client mailbox
		- content->group_speed: Group speed		
		- content->speed: Host speed
		- content->percentage: Percentage of project (in relation to all projects) 
	
	INPUT FILE REQUEST NEEDS:

		- type: REQUEST
		- answer_mailbox: Client mailbox 

	EXECUTION RESULTS REPLY NEEDS:

		- type: REPLY
		- content: reply_t
		- content->result_number: executed result number
		- content->workunit: associated workunit
		- content->credits: number of credits to request

	OUTPUT FILE REPLY NEEDS:

		- type: REPLY

	*/
	pdatabase_t database = NULL;

	// Scheduling server work request
	msg_task_t sswork_request_task = NULL;		// Work request task to scheduling server
	msg_task_t sswork_reply_task = NULL;		// Work reply task from scheduling server
	ssmessage_t sswork_request = NULL;		// Work request message
	result_t sswork_reply = NULL;			// Work reply message
	
	// Data server input file request
	msg_task_t dsinput_file_request_task = NULL;	// Input file request task to data server
	msg_task_t dsinput_file_reply_task = NULL;	// Input file reply task from data server
	dsmessage_t dsinput_file_request = NULL;	// Input file request message

	// Scheduling server execution results reply
	msg_task_t ssexecution_results_task = NULL;	// Execution results task to scheduling server
	ssmessage_t ssexecution_results = NULL;		// Execution results message

	// Data server output file reply
	msg_task_t dsoutput_file_task = NULL;		// Output file task to data server
	dsmessage_t dsoutput_file = NULL;		// Output file message

	char *server_name = NULL;			// Store data server name 
	msg_comm_t comm = NULL;				// Asynchronous communication
	int32_t i;					// Index
	double rand = 0;

	database = &_pdatabase[(int)proj->number];	// Boinc server info pointer	
		
	// Check if there are executed results
	while(proj->total_tasks_executed > proj->total_tasks_checked){
		// Create execution results message
		ssexecution_results = xbt_new0(s_ssmessage_t, 1);
		ssexecution_results->type = REPLY;
		ssexecution_results->content = xbt_new(s_reply_t, 1);

		// Increase number of tasks checked
		proj->total_tasks_checked++;	
		
		// Pop executed result number and associated workunit
		xbt_queue_pop(proj->number_executed_task, &((reply_t)ssexecution_results->content)->result_number);
		xbt_queue_pop(proj->workunit_executed_task, &((reply_t)ssexecution_results->content)->workunit);
		
		workunit_t workunit = xbt_dict_get(database->current_workunits, ((reply_t)ssexecution_results->content)->workunit);
		application_t application = &database->applications[workunit->application];
		// Executed task status [SUCCES, FAIL]	
		if(uniform_int(0,99) < application->success_percentage){
			 ((reply_t)ssexecution_results->content)->status = SUCCESS;
			// Executed task value [CORRECT, INCORRECT]
			if(uniform_int(0,99) < application->canonical_percentage) {
				((reply_t)ssexecution_results->content)->value = CORRECT;
			} 
			else ((reply_t)ssexecution_results->content)->value = INCORRECT;
		}
		else{
			((reply_t)ssexecution_results->content)->status = FAIL;
			((reply_t)ssexecution_results->content)->value = INCORRECT;
		}
	
	
		// Calculate credits	
		((reply_t)ssexecution_results->content)->credits = (int32_t)((int64_t)application->job_duration / 1000000000.0 * CREDITS_CPU_S);	
		// Create execution results task
		ssexecution_results_task = MSG_task_create("execution_answer", 0, REPLY_SIZE, ssexecution_results);
			
		// Send message to the server
		MSG_task_send(ssexecution_results_task, database->scheduling_servers[uniform_int(0, database->nscheduling_servers-1)]);
		ssexecution_results_task = NULL;

		// Upload output file to data server
		dsoutput_file = xbt_new0(s_dsmessage_t, 1);	
		dsoutput_file->type = REPLY;
		dsoutput_file_task = MSG_task_create("output_file", 0, application->output_file_size, dsoutput_file);			
		MSG_task_send(dsoutput_file_task, database->data_servers[uniform_int(0, database->ndata_servers-1)]);
		dsoutput_file_task = NULL;
	}

	// Request work
		sswork_request = xbt_new0(s_ssmessage_t, 1);
		sswork_request->type = REQUEST;
		sswork_request->content = xbt_new(s_request_t, 1);
		((request_t)sswork_request->content)->answer_mailbox = proj->answer_mailbox;
		((request_t)sswork_request->content)->group_speed = _group_info[client->group_number].group_speed;
		((request_t)sswork_request->content)->speed = client->speed;
		((request_t)sswork_request->content)->percentage = percentage;	
		sswork_request_task = MSG_task_create("ask_addr", 0, REQUEST_SIZE, sswork_request);			
		MSG_task_send(sswork_request_task, database->scheduling_servers[uniform_int(0, database->nscheduling_servers-1)]);	
		MSG_task_receive(&sswork_reply_task, proj->answer_mailbox);	// Receive reply from scheduling server
		comm = xbt_dict_get(_sscomm, proj->answer_mailbox);		// Get connection
		xbt_dict_remove(_sscomm, proj->answer_mailbox);			// Remove connection from dict
		MSG_comm_wait(comm, -1);					// Wait until communication ends	
		MSG_comm_destroy(comm);						// Destroy connection
		sswork_reply = (result_t)MSG_task_get_data(sswork_reply_task);	// Get work
		comm = NULL;	

		// Download input files (or generate them locally)
		if (sswork_reply->number_tasks > 0) {
			application_t application = &database->applications[sswork_reply->workunit->application];
			if(uniform_int(0,99) < (int)application->ifgl_percentage){
				// Download only if the workunit was not downloaded previously
				if(uniform_int(0,99) < (int)application->ifcd_percentage){
					dsinput_file_request = xbt_new0(s_dsmessage_t, 1);
					dsinput_file_request->type = REQUEST;
					dsinput_file_request->application = sswork_reply->workunit->application;
					dsinput_file_request->answer_mailbox = proj->answer_mailbox;
					dsinput_file_request_task = MSG_task_create("ask_work", 0, KB, dsinput_file_request);
					server_name = sswork_reply->input_files[0];
					MSG_task_send(dsinput_file_request_task, server_name);			// Send input file request
					MSG_task_receive(&dsinput_file_reply_task, proj->answer_mailbox);	// Send input file reply
					comm = xbt_dict_get(_dscomm, proj->answer_mailbox);			// Get connection
					xbt_dict_remove(_dscomm, proj->answer_mailbox);				// Remove connection from dict
					MSG_comm_wait(comm, -1);						// Wait until communication ends
					MSG_comm_destroy(comm);							// Destroy connection
					comm = NULL;
					MSG_task_destroy(dsinput_file_reply_task);
				}
			}
		}

		if(sswork_reply->number_tasks == 0) {
			proj->on = 0;
			rand = uniform_ab(1.9, 2.1);
			client->work_fetch_multiplicator = min(MAX_WORK_FETCH_MULTIPLICATOR, client->work_fetch_multiplicator * rand);
		} else {
			proj->on = 1;
			client->work_fetch_multiplicator = 1.0;
		}
		// Insert received tasks in tasks swag	
		for (i = 0; i < (int)sswork_reply->number_tasks; i++) {
			task_t t = sswork_reply->tasks[i];
			t->msg_task = MSG_task_create(t->name, t->duration, 0, t);
			t->project = proj;
			xbt_swag_insert_at_tail(t, proj->tasks);
		}

		// Increase the total number of tasks received
		proj->total_tasks_received = proj->total_tasks_received + sswork_reply->number_tasks;

		// Free
		xbt_free(sswork_reply->tasks);
		xbt_free(sswork_reply);
		MSG_task_destroy(sswork_reply_task);
	
	// Signal main client process 
	client->on = 0;	
	xbt_cond_signal(client->sched_cond);

	return 0;
}

/*****************************************************************************/
/* update shortfall(amount of work needed to keep 1 cpu busy during next ConnectionInterval) of client */
static void client_update_shortfall(client_t client)
{
	//printf("Executing client_update_shortfall\n");
	task_t task;
	xbt_dict_cursor_t cursor = NULL;
	char *key;
	project_t proj;
	xbt_dict_t projects = client->projects;
	double total_time_proj;
	double total_time = 0;
	int64_t speed; // (maximum 2⁶³-1)

	client->no_actions = 1;
	speed = client->speed;
	xbt_dict_foreach(projects, cursor, key, proj) {
		total_time_proj = 0;
		xbt_swag_foreach(task, proj->tasks) {
			total_time_proj += (MSG_task_get_remaining_computation(task->msg_task)*client->factor)/speed;

	//printf("SHORTFALL(1) %g   %s    %g   \n",  MSG_get_clock(), proj->name,   MSG_task_get_remaining_computation(task->msg_task));
			client->no_actions = 0;
		}
		xbt_swag_foreach(task, proj->run_list) {
			total_time_proj += (MSG_task_get_remaining_computation(task->msg_task)*client->factor)/speed;
			client->no_actions = 0;
	//printf("SHORTFALL(2) %g  %s    %g   \n",  MSG_get_clock(), proj->name,   MSG_task_get_remaining_computation(task->msg_task));
		}
		total_time += total_time_proj;
		/* amount of work needed - total already loaded */
		//printf("shortfall %0.1f %0.1f\n", _group_info[client->group_number].connection_interval*proj->priority/ client->sum_priority, total_time_proj);
		proj->shortfall = _group_info[client->group_number].connection_interval*proj->priority/ client->sum_priority - total_time_proj;


		if (proj->shortfall < 0) {
			//printf("shortfall %0.1f %0.1f\n", _group_info[client->group_number].connection_interval*proj->priority/ client->sum_priority, total_time_proj);
			proj->shortfall = 0;
		}
	}
	client->total_shortfall = _group_info[client->group_number].connection_interval - total_time;
	if (client->total_shortfall < 0)
		client->total_shortfall = 0;

}


/*
	Client work fetch
*/
static int client_work_fetch(int argc, char *argv[])
{
	xbt_ex_t e;
	project_t selected_proj = NULL;
	xbt_dict_cursor_t cursor = NULL;
	char *key;
	static char first = 1;
	project_t proj;	
	double work_percentage = 0;
	double control, sleep;
	
	MSG_process_sleep(uniform_ab(0,3600));

	client_t client = MSG_process_get_data(MSG_process_self());
	xbt_dict_t projects = client->projects;

	//printf("Running thread work fetch client %s\n", client->name);

	xbt_mutex_acquire(client->mutex_init);
        while (client->initialized == 0)
                xbt_cond_wait(client->cond_init, client->mutex_init);
        xbt_mutex_release(client->mutex_init);

	while (ceil(MSG_get_clock()) < sim_duration) {

		/* Wait when the client is suspended */ 
		xbt_mutex_acquire(client->ask_for_work_mutex);
		while(client->suspended){
			sleep = client->suspended;
			client->suspended = 0;
			xbt_mutex_release(client->ask_for_work_mutex);
			MSG_process_sleep(sleep);
			continue;		
		}
		xbt_mutex_release(client->ask_for_work_mutex);

		client_update_shortfall(client);
		
		selected_proj = NULL;
		xbt_dict_foreach(projects, cursor, key, proj) {
			/* if there are no running tasks so we can download from all projects. Don't waste processing time */
			//if (client->running_project != NULL && client->running_project->running_task && proj->long_debt < -_group_speed[client->group_number].scheduling_interval) {
			//printf("Shortfall %s: %f\n", proj->name, proj->shortfall);
			/*if(!proj->on && proj->total_tasks_executed == proj->total_tasks_checked){
				continue;
			}*/
			if (!client->no_actions && proj->long_debt < -_group_info[client->group_number].scheduling_interval) {
				printf("continue1\n");
				continue;
			}
			if (proj->shortfall == 0) {
				//printf("continue2\n");
				continue;
			}
	
			/* FIXME: CONFLIT: the article says (long_debt - shortfall) and the wiki(http://boinc.berkeley.edu/trac/wiki/ClientSched) says (long_debt + shortfall). I will use here the wiki definition because it seems have the same behavior of web client simulator.*/

///////******************************///////
			if ((selected_proj == NULL) || (control < (proj->long_debt + proj->shortfall)) ) {
				control = proj->long_debt + proj->shortfall;
				selected_proj = proj;
			}
			if (fabs(control - proj->long_debt - proj->shortfall) < PRECISION) {
				control = proj->long_debt + proj->shortfall;
				selected_proj = proj;
			}
		}

		if (selected_proj) {
			//printf("Selected project(%s) shortfall %lf %d\n", selected_proj->name, selected_proj->shortfall, selected_proj->shortfall > 0);
			/* prrs = sum_priority, all projects are potentially runnable */
			work_percentage = selected_proj->shortfall > client->total_shortfall/client->sum_priority ? selected_proj->shortfall : client->total_shortfall/client->sum_priority;
			//printf("%s -> work_percentage: %f\n", selected_proj->name, work_percentage); // SAUL
			//printf("Heap size: %d\n", xbt_heap_size(client->deadline_missed)); // SAUL
			
			/* just ask work if there aren't deadline missed jobs 
FIXME: http://www.boinc-wiki.info/Work-Fetch_Policy */
			if (xbt_heap_size(client->deadline_missed) == 0 && work_percentage > 0)
			{
				//printf("*************    ASK FOR WORK      %g   %g\n",   work_percentage, MSG_get_clock());
				if (!selected_proj->on && selected_proj->total_tasks_executed == selected_proj->total_tasks_checked)	{
					MSG_process_sleep((client->work_fetch_multiplicator - 1) * WORK_FETCH_PERIOD);
				}
				//printf("asking for work %0.1f\n", MSG_get_clock());
				client_ask_for_work(client, selected_proj, work_percentage);		
			} 
		}
		/* workaround to start scheduling tasks at time 0 */
		if (first) {
			//printf(" work_fetch: %g\n", MSG_get_clock());
			client->on = 0;	
			xbt_cond_signal(client->sched_cond);
			first = 0;
		}

		TRY {
			if(MSG_get_clock() >= (sim_duration-WORK_FETCH_PERIOD))
				break;
			if (!selected_proj || xbt_heap_size(client->deadline_missed) > 0 || work_percentage == 0) {
				//printf("EXIT 1 %d %d %0.1f\n", (int)selected_proj, xbt_heap_size(client->deadline_missed), work_percentage);
				xbt_cond_timedwait(client->work_fetch_cond, client->work_fetch_mutex, max(0, sim_duration-MSG_get_clock()));
				//xbt_cond_timedwait(client->work_fetch_cond, client->work_fetch_mutex, -1);
				//printf("SALGO DE EXIT 1: remaining %f, time %f\n", sim_duration-MSG_get_clock(), MSG_get_clock());
			}
			else{
				//printf("sleeping %0.1f\n", client->work_fetch_multiplicator * WORK_FETCH_PERIOD);
				//printf("EXIT 2: remaining %f time %f\n", sim_duration-MSG_get_clock(), MSG_get_clock());
				xbt_cond_timedwait(client->work_fetch_cond, client->work_fetch_mutex,  WORK_FETCH_PERIOD);
				//printf("SALGO DE EXIT 2: remaining %f, time %f\n", sim_duration-MSG_get_clock(), MSG_get_clock());
			}
		} CATCH (e) {
			xbt_ex_free(e);
			//printf("Error %d %d\n", (int)MSG_get_clock(), (int)sim_duration); 
		}
	}	

	// Sleep until max simulation time
	if(MSG_get_clock() < sim_duration)
		MSG_process_sleep(sim_duration-MSG_get_clock());	

	// Signal main client thread
	xbt_mutex_acquire(client->ask_for_work_mutex);	
	client->suspended = -1;
	xbt_cond_signal(client->ask_for_work_cond);
	xbt_mutex_release(client->ask_for_work_mutex);

	//printf("Finished work_fetch %s: %d in %f\n", client->name, client->finished, MSG_get_clock());

	return 0;
}

/*****************************************************************************/
/* Update client short and long term debt.
This function is called every schedulingInterval and when an action finishes
The wall_cpu_time must be updated when this function is called */
static void client_clean_short_debt(const client_t client)
{
	xbt_dict_cursor_t cursor = NULL;
	char *key;
	project_t proj;
	xbt_dict_t projects = client->projects;

	/* calcule a */
	xbt_dict_foreach(projects, cursor, key, proj) {
		proj->short_debt = 0;
		proj->wall_cpu_time = 0;
	}

}

static void client_update_debt(client_t client)
{
	double a, w, w_short;
	double total_debt_short = 0;
	double total_debt_long = 0;
	xbt_dict_cursor_t cursor = NULL;
	char *key;
	project_t proj;
	xbt_dict_t projects = client->projects;
	a = 0;
	double sum_priority_run_proj = 0;  /* sum of priority of runnable projects, used to calculate short_term debt */
	int num_project_short = 0;

	/* calcule a */
	xbt_dict_foreach(projects, cursor, key, proj) {
		a += proj->wall_cpu_time;
		if (xbt_swag_size(proj->tasks) > 0 || xbt_swag_size(proj->run_list) > 0) {
			sum_priority_run_proj += proj->priority;
			num_project_short++;
		}
	}

	/* update short and long debt */	
	xbt_dict_foreach(projects, cursor, key, proj) {
		w = a * proj->priority/client->sum_priority;
		w_short = a * proj->priority/sum_priority_run_proj;
		//printf("Project(%s) w=%lf a=%lf wall=%lf\n", proj->name, w, a, proj->wall_cpu_time);

		proj->short_debt += w_short - proj->wall_cpu_time;
		proj->long_debt += w - proj->wall_cpu_time;
		/* http://www.boinc-wiki.info/Short_term_debt#Short_term_debt 
		 * if no actions in project short debt = 0 */
		if (xbt_swag_size(proj->tasks) == 0 && xbt_swag_size(proj->run_list) == 0)
			proj->short_debt = 0;
		total_debt_short += proj->short_debt;
		total_debt_long += proj->long_debt;
	}

	/* normalize short_term */
	xbt_dict_foreach(projects, cursor, key, proj) {
	//	proj->long_debt -= (total_debt_long / xbt_dict_size(projects));

		//printf("Project(%s), long term debt: %lf, short term debt: %lf\n", proj->name, proj->long_debt, proj->short_debt);
		/* reset wall_cpu_time */
		proj->wall_cpu_time = 0;

		if (xbt_swag_size(proj->tasks) == 0 && xbt_swag_size(proj->run_list) == 0)
			continue;
		proj->short_debt -= (total_debt_short / num_project_short);
		if (proj->short_debt > MAX_SHORT_TERM_DEBT)
			proj->short_debt = MAX_SHORT_TERM_DEBT;
		if (proj->short_debt < -MAX_SHORT_TERM_DEBT)
			proj->short_debt = -MAX_SHORT_TERM_DEBT;
	}

}

/*****************************************************************************/
/* verify whether the task will miss its deadline if it executes alone on cpu */
static int deadline_missed(task_t task)
{
	int64_t speed; // (maximum 2⁶³-1)
	double remains;
	speed = task->project->client->speed;
	remains = MSG_task_get_remaining_computation(task->msg_task)*task->project->client->factor;
	/* we're simulating only one cpu per host */
	if (MSG_get_clock() + (remains/speed) > task->start + task->deadline){
		//printf("speed: %ld\n", speed);
		//printf("remains: %f\n", remains);
		//printf("deadline_missed\n");
		return 1;
	}
	return 0;
}

/* simulate task scheduling and verify if it will miss its deadline */
static int task_deadline_missed_sim(client_t client, project_t proj, task_t task)
{
	return task->sim_finish > (task->start + task->deadline - _group_info[client->group_number].scheduling_interval)*0.9;
}

static void client_update_simulate_finish_time(client_t client)
{
	xbt_dict_cursor_t cursor = NULL;
	char *key;
	project_t proj;
	int total_tasks = 0;
	double clock_sim = MSG_get_clock();
	int64_t speed = client->speed;
	xbt_dict_t projects = client->projects;

	xbt_dict_foreach(projects, cursor, key, proj) {
		task_t task;
		total_tasks += xbt_swag_size(proj->tasks) + xbt_swag_size(proj->run_list);

		xbt_swag_foreach(task, proj->tasks) {
			task->sim_remains = MSG_task_get_remaining_computation(task->msg_task)*client->factor;
			xbt_swag_insert(task, proj->sim_tasks);
		}
		xbt_swag_foreach(task, proj->run_list) {
			task->sim_remains = MSG_task_get_remaining_computation(task->msg_task)*client->factor;
			xbt_swag_insert(task, proj->sim_tasks);
		}
	}
	//printf("Total tasks %d\n", total_tasks);

	while (total_tasks) {
		double sum_priority = 0.0;
		task_t min_task = NULL;
		double min = 0.0;

		/* sum priority of projects with tasks to execute */
		xbt_dict_foreach(projects, cursor, key, proj) {
			if (xbt_swag_size(proj->sim_tasks) > 0)
				sum_priority += proj->priority;
		}

		/* update sim_finish and find next action to finish */
		xbt_dict_foreach(projects, cursor, key, proj) {
			task_t task;
			xbt_swag_foreach(task, proj->sim_tasks) {
				task->sim_finish = clock_sim + (task->sim_remains/speed)*(sum_priority/proj->priority)*xbt_swag_size(proj->sim_tasks);
				if (min_task == NULL || min > task->sim_finish) {
					min = task->sim_finish;
					min_task = task;
				}
			}
		}

		//printf("En %g  Task(%s)(%p):Project(%s) amount %lf remains %lf sim_finish %lf deadline %lf\n", MSG_get_clock(), min_task->name, min_task, min_task->project->name, min_task->duration, min_task->sim_remains, min_task->sim_finish, min_task->start + min_task->deadline);
		/* update remains of tasks */
		xbt_dict_foreach(projects, cursor, key, proj) {
			task_t task;
			xbt_swag_foreach(task, proj->sim_tasks) {
				task->sim_remains -= (min - clock_sim)*speed*(proj->priority/sum_priority)/xbt_swag_size(proj->sim_tasks);
			}
		}
		/* remove action that has finished */
		total_tasks--;
		xbt_swag_remove(min_task, min_task->project->sim_tasks);
		clock_sim = min;
	}

}

/* verify whether the actions in client's list will miss their deadline and put them in client->deadline_missed */
static void client_update_deadline_missed(client_t client)
{
	task_t task, task_next;
	xbt_dict_cursor_t cursor = NULL;
	char *key;
	project_t proj;
	xbt_dict_t projects = client->projects;

	client_update_simulate_finish_time(client);

	xbt_dict_foreach(projects, cursor, key, proj) {
		xbt_swag_foreach_safe(task, task_next, proj->tasks) {
			if (task->heap_index >= 0)
				xbt_heap_remove(client->deadline_missed, task->heap_index);

			if (task_deadline_missed_sim(client, proj, task)) {
				//printf("Client(%s), Project(%s), Possible Deadline Missed task(%s)(%p)\n", client->name, proj->name, MSG_task_get_name(task->msg_task), task);
				xbt_heap_push(client->deadline_missed, task, (task->start + task->deadline));
				//printf("((((((((((((((  HEAP PUSH      1   heap index %d\n", task->heap_index);
			}
		}
		xbt_swag_foreach_safe(task, task_next, proj->run_list) {
			if (task->heap_index >= 0)
				xbt_heap_remove(client->deadline_missed, task->heap_index);
			if (task_deadline_missed_sim(client, proj, task)) {
				//printf("Client(%s), Project(%s), Possible Deadline Missed task(%s)(%p)\n", client->name, proj->name, MSG_task_get_name(task->msg_task), task);
				xbt_heap_push(client->deadline_missed, task, task->start + task->deadline);
				//printf("((((((((((((((  HEAP PUSH      2ii     heap index %d \n", task->heap_index);
			}
		}
	}
}

/*****************************************************************************/

/* void function, we don't need the enforcement policy since we don't simulate checkpointing and the deadlineMissed is updated at client_update_deadline_missed */
static void client_enforcement_policy()
{
	return;
}

static void schedule_job(client_t client, task_t job)
{
	/* task already running, just return */
	if (job->running) {
		if (client->running_project != job->project) {
			MSG_process_suspend(client->running_project->thread);
			MSG_process_resume(job->project->thread);


//printf("=============  Suspend   %s     resume    %s  %g\n",   client->running_project->name, 			 job->project->name, MSG_get_clock());

			client->running_project = job->project;
		}
		return;
	}
	/* schedule task */
	if (!job->scheduled) {
		xbt_queue_push(job->project->tasks_ready, &job);
		job->scheduled = 1;
	} 
	/* if a task is running, cancel it and create new MSG_task */
	if (job->project->running_task != NULL) {
		double remains;			
		task_t task_temp = job->project->running_task;
		remains = MSG_task_get_remaining_computation(task_temp->msg_task)*client->factor;
		MSG_task_cancel(task_temp->msg_task);
		MSG_task_destroy(task_temp->msg_task);
		task_temp->msg_task = MSG_task_create(task_temp->name, remains, 0, task_temp);

		//printf("Creating task(%s)(%p) again, remains %lf\n", task_temp->name, task_temp, remains);


		task_temp->scheduled = 0;
		task_temp->running = 0;
	}
	/* stop running thread and start other */
	if (client->running_project) {
		MSG_process_suspend(client->running_project->thread);
//printf("=============  Suspend   %s     %g  \n",   client->running_project->name, MSG_get_clock());
	}
		MSG_process_resume(job->project->thread);

//printf("====================       resume    %s     %g\n",    job->project->name, MSG_get_clock());

		client->running_project = job->project;
}
/*****************************************************************************/
/* this function is responsible to schedule the right task for the next SchedulingInterval.
	 We're simulating only one cpu per host, so when this functions schedule a task it's enought for this turn
FIXME: if the task finish exactly at the same time of this function is called (i.e. remains = 0). We loose a schedulingInterval of processing time, cause we schedule it again */
static void client_cpu_scheduling(client_t client)
{
	task_t task = NULL;
	xbt_dict_cursor_t cursor = NULL;
	char *key;
	project_t proj, great_debt_proj = NULL;
	xbt_dict_t projects = client->projects;
	double great_debt;

#if 0
	xbt_dict_foreach(projects, cursor, key, proj) {
		proj->anticipated_debt = proj->short_debt;
	}	
#endif

	/* schedule EDF task */ 
	/* We need to preemt the actions that may be executing on cpu, it is done by cancelling the action and creating a new one (with the remains amount updated) that will be scheduled later */
			//printf("-------------------   1 %g\n", MSG_get_clock());
	while ((task = xbt_heap_pop(client->deadline_missed)) != NULL) {

			//printf("-------------------   2\n");

		if (deadline_missed(task)) {
			//printf("Task-1(%s)(%p) from project(%s) deadline, skip it\n", task->name, task, task->project->name);
			//printf("-------------------3\n");
	
			task->project->total_tasks_missed = task->project->total_tasks_missed +1;
			
			client_update_debt(client);		//FELIX
			client_clean_short_debt(client);	// FELIX
			free_task(task);


			//printf("===================4\n");


			continue;
		}


		//printf("Client (%s): Scheduling task(%s)(%p) of project(%s)\n", client->name, MSG_task_get_name(task->msg_task), task, task->project->name);
		// update debt (anticipated). It isn't needed due to we only schedule one job per host.
		if (xbt_swag_belongs(task, task->project->tasks))
			xbt_swag_remove(task, task->project->tasks);

		xbt_swag_insert(task, task->project->run_list);

		/* keep the task in deadline heap */
				//printf("((((((((((((((  HEAP PUSH      3\n");
		xbt_heap_push(client->deadline_missed, task, (task->start + task->deadline));
		schedule_job(client, task);
		return;
	}


	while (1) {
			//printf("==============================  5\n");
		great_debt_proj = NULL;
		task = NULL;
		xbt_dict_foreach(projects, cursor, key, proj) {
			if ( ((great_debt_proj == NULL) || (great_debt < proj->short_debt)) && (xbt_swag_size(proj->run_list) || xbt_swag_size(proj->tasks))) {
				great_debt_proj = proj;
				great_debt = proj->short_debt;
			}
		}
		
		if (!great_debt_proj)
		{
			//printf(" scheduling: %g\n", MSG_get_clock());
			//xbt_cond_signal(proj->client->work_fetch_cond);   //FELIX
			proj->client->on = 1;	
                        xbt_cond_signal(proj->client->sched_cond);   //FELIX
			//printf("salgo por aquiiiiiiiiiiiiiiiii  \n");
			return;
		}

		/* get task already running or first from tasks list */
		if ((task = xbt_swag_extract(great_debt_proj->run_list)) != NULL) {
			/* reinsert at swag */
			xbt_swag_insert(task, great_debt_proj->run_list);
		}
		else if ((task = xbt_swag_extract(great_debt_proj->tasks)) != NULL) {
			xbt_swag_insert(task, great_debt_proj->run_list);
		}
		if (task) {
			if (deadline_missed(task)) {
				//printf(">>>>>>>>>>>> Task-2(%s)(%p) from project(%s) deadline, skip it\n", task->name, task, task->project->name);
				free_task(task);
				continue;
			}
			//printf("Client (%s): Scheduling task(%s)(%p) of project(%s)\n", client->name, MSG_task_get_name(task->msg_task), task, task->project->name);

			schedule_job(client, task);
		}
		client_enforcement_policy();
		return;
	}
}

/*****************************************************************************/

int client_execute_tasks(int argc, char *argv[])
{
	task_t task;
	msg_error_t err;
	project_t proj = MSG_process_get_data(MSG_process_self());
	int32_t number;

	//printf("Running thread to execute tasks from project %s in %s  %g\n", proj->name, 			MSG_host_get_name(MSG_host_self()),  MSG_get_host_speed(MSG_host_self()));

	/* loop, execute tasks from queue until they finish or are cancelled by the main thread */
	while (1) {	
		xbt_queue_pop(proj->tasks_ready, &task);			

		//printf("TERMINO POP %s EN %f\n", proj->client->name, MSG_get_clock());
		//printf("%s Executing task(%s)(%p)\n", proj->client->name, task->name, task);
		xbt_cond_signal(proj->client->work_fetch_cond);
		task->running = 1;
		proj->running_task = task;
		grid_busy(proj->client);
		/* task finishs its execution, free structures */

		//printf("----(1)-------Task(%s)(%s) from project(%s) start  duration = %g   speed=  %g %d\n", task->name, task, proj->name,  MSG_task_get_compute_duration(task->msg_task), MSG_get_host_speed(MSG_host_self()), MSG_get_clock(), MSG_host_get_core_number(MSG_host_self()));

		//t0 = MSG_get_clock();

		err = MSG_task_execute(task->msg_task);
		
		//printf("Tarea ejecutada\n");
		
		if (err == MSG_OK) {
			number = (int32_t)atoi(task->name);
			//printf("s%d TERMINO EJECUCION DE %d en %f\n", proj->number, number, MSG_get_clock());
			xbt_queue_push(proj->number_executed_task, &number);
			xbt_queue_push(proj->workunit_executed_task, &(task->workunit));
			proj->total_tasks_executed++;
			//printf("%f\n", proj->client->workunit_executed_task);
		//t1 = MSG_get_clock();

		//printf("-----(3)------Task(%s)(%s) from project(%s) finished, le queda %g --   cuanto  %g  %g %d\n", task->name, task, proj->name, MSG_task_get_remaining_computation(task->msg_task), t1-t0 ,MSG_get_clock());   		

			task->running = 0;
			proj->wall_cpu_time += MSG_get_clock() - proj->client->last_wall;
			proj->client->last_wall = MSG_get_clock();
			client_update_debt(proj->client);
			client_clean_short_debt(proj->client);

			proj->running_task = NULL;
			grid_idle(proj->client);
			free_task(task);
			
			proj->client->on = 1;	
			xbt_cond_signal(proj->client->sched_cond);			
			continue;
		}

		printf("%f: ---(2)--------Task(%s)(%p) from project(%s) error finished  duration = %g   speed=  %g\n", MSG_get_clock(), task->name, task, proj->name,  MSG_task_get_compute_duration(task->msg_task), MSG_get_host_speed(MSG_host_self()));
		task->running = 0;
		proj->running_task = NULL;
		grid_idle(proj->client);
		free_task(task);
		continue;
	}
	
	proj->thread = NULL;

	//printf("Finished execute_tasks %s in %f\n", proj->client->name, MSG_get_clock());

	return 0;
}

/*****************************************************************************/

static client_t client_new(int argc, char *argv[])
{
	client_t client;
	char * work_string;
	char *key;
	xbt_dict_cursor_t cursor = NULL;
	project_t proj;
	int32_t group_number;
	double r = 0, aux = -1;
	int index = 1;

	client = xbt_new0(s_client_t, 1);

	client->group_number = group_number = (int32_t)atoi(argv[index++]);

	// Initialize values
	if(argc > 3)
	{
		_group_info[group_number].group_speed = (int32_t) MSG_get_host_speed(MSG_host_self()); 
		_group_info[group_number].n_clients = (int32_t)atoi(argv[index++]);		
		_group_info[group_number].connection_interval = atof(argv[index++]);
		_group_info[group_number].scheduling_interval = atof(argv[index++]);
		_group_info[group_number].max_speed = atof(argv[index++]);
		_group_info[group_number].min_speed = atof(argv[index++]);
		_group_info[group_number].sp_distri = (char)atoi(argv[index++]);
		_group_info[group_number].sa_param = atof(argv[index++]);
		_group_info[group_number].sb_param = atof(argv[index++]);
		_group_info[group_number].av_distri = (char) atoi(argv[index++]);
		_group_info[group_number].aa_param = atof(argv[index++]);
		_group_info[group_number].ab_param = atof(argv[index++]);
		_group_info[group_number].nv_distri = (char) atoi(argv[index++]);
		_group_info[group_number].na_param = atof(argv[index++]);
		_group_info[group_number].nb_param = atof(argv[index++]);
		_group_info[group_number].tail_mean_speed = atof(argv[index++]);
		_group_info[group_number].tail_availability = atof(argv[index++]);
		if((argc-19)%3 != 0){ 
			aux = atof(argv[index++]);
		}
		_group_info[group_number].proj_args = &argv[index];
		_group_info[group_number].on = argc - index;	

		xbt_cond_signal(_group_info[group_number].cond);	
	}else{
		xbt_mutex_acquire(_group_info[group_number].mutex);
		while(_group_info[group_number].on == 0)
			xbt_cond_wait(_group_info[group_number].cond, _group_info[group_number].mutex);
		xbt_mutex_release(_group_info[group_number].mutex);
		if(argc == 3) aux = atof(argv[index]);
	}

	if(aux == -1){
		aux = ran_distri(_group_info[group_number].sp_distri, _group_info[group_number].sa_param, _group_info[group_number].sb_param);  
		if(aux > _group_info[group_number].max_speed)
			aux = _group_info[group_number].max_speed;
		else if(aux < _group_info[group_number].min_speed)
			aux = _group_info[group_number].min_speed;
	}
	FILE* speed_statistics_file = fopen("../exp/speed_statistics", "a+");
	fprintf(speed_statistics_file, "%0.1f\n", aux);
	fclose(speed_statistics_file);

	client->speed = (int64_t)(aux*1000000000.0);

	client->factor = (double)client->speed/_group_info[group_number].group_speed;

	client->name = MSG_host_get_name(MSG_host_self());

	client_initialize_projects(client, _group_info[group_number].on, _group_info[group_number].proj_args);
	client->deadline_missed = xbt_heap_new(8, NULL);  // FELIX, antes había 8

	//printf("Client speed: %f GFLOPS\n", client->speed/1000000000.0);

	xbt_heap_set_update_callback(client->deadline_missed, task_update_index);

	client->on = 0;	
	client->running_project = NULL;

	// Suspender a work_fetch_thread cuando la máquina se cae
	client->ask_for_work_mutex = xbt_mutex_init();
	client->ask_for_work_cond = xbt_cond_init();
	client->suspended = 0;
	
	client->sched_mutex = xbt_mutex_init();
	client->sched_cond = xbt_cond_init();
	client->work_fetch_mutex = xbt_mutex_init();
	client->work_fetch_cond = xbt_cond_init();

	client->finished = 0;

	client->mutex_init = xbt_mutex_init();
	client->cond_init = xbt_cond_init();
	client->initialized = 0;
	client->n_projects = 0;
	client->work_fetch_multiplicator = 1;
	client->stats_online = 0;
	client->stats_idle = 0;

	double join_time = client->join_day * 24 + uniform_ab(0, min(24, (sim_duration - MSG_get_clock()) / 3600.0));
	MSG_process_sleep(join_time * 3600);

	work_string = bprintf("work_fetch:%s\n", client->name);
	client->work_fetch_thread = MSG_process_create(work_string, client_work_fetch, client, MSG_host_self());
	xbt_free(work_string);

	//printf("Starting client %s, ConnectionInterval %lf SchedulingInterval %lf\n", client->name, _group_info[client->group_number].connection_interval, _group_speed[client->group_number].scheduling_interval);

	/* start one thread to each project to run tasks */
	xbt_dict_foreach(client->projects, cursor, key, proj) {
		char *proj_name;
		proj_name = bprintf("%s:%s\n", key, client->name);
		if ((proj->thread = MSG_process_create(proj_name, client_execute_tasks, proj, MSG_host_self())) == NULL) {
			printf("Error creating thread\n");
			xbt_abort();
		}
		r += proj->priority;
		xbt_free(proj_name);
		client->n_projects++;
	}

	xbt_mutex_acquire(client->mutex_init);
	client->sum_priority = r;
	client->initialized = 1;
	xbt_cond_signal(client->cond_init);
	xbt_mutex_release(client->mutex_init);

	return client;
}

// Main client function
int client(int argc, char *argv[])
{
	client_t client;
	project_t proj;
	msg_task_t task;
	ssmessage_t msg;
	xbt_ex_t e;
	xbt_dict_cursor_t cursor = NULL;
	char *key;
	int working = 0, i;
	int time_sim = 0;
	int64_t speed;
	double time = 0, random = 0;
	double available = 0, notavailable = 0;
	double time_wait;

	client = client_new(argc, argv);
	speed = client->speed;
	
	
	//printf("Starting client %s\n", client->name);

	while (ceil(MSG_get_clock()) < sim_duration) {
		//printf("%s finished: %d, nprojects: %d en %f\n", client->name, client->finished, client->n_projects, MSG_get_clock());
#if 1
		if(!working){
			working = 1;
			pdatabase_t database = &_pdatabase[0];
			database->clients_availability[(int32_t)MSG_get_clock()] += 1;
			grid_online(client);
			random = (ran_distri(_group_info[client->group_number].av_distri, _group_info[client->group_number].aa_param, _group_info[client->group_number].ab_param)*3600.0);
			random = max(random, 0);
			if(ceil(random + MSG_get_clock()) >= sim_duration){
				//printf("%f\n", random);
				random = (double)max(sim_duration - MSG_get_clock(), 0);
			}
			FILE* availability = fopen("../exp/availability", "a+");
			fprintf(availability, "%0.1f\n", random / 3600);
			fclose(availability);
			//printf("%0.1f\n", random);
			available+=random;
			//printf("Weibull: %f\n", random);
			time = MSG_get_clock() + random;
			database->clients_availability[(int32_t)time] -= 1;
		}
#endif	

		/* increase wall_cpu_time to the project running task */
		if (client->running_project && client->running_project->running_task) {
			client->running_project->wall_cpu_time += MSG_get_clock() - client->last_wall;
			client->last_wall = MSG_get_clock();
		}

		// SAUL
		client_update_debt(client);
		client_update_deadline_missed(client);
		client_cpu_scheduling(client);
		
		if(client->on)
			xbt_cond_signal(client->work_fetch_cond);


/*************** SIMULAR CAIDA DEL CLIENTE ****/

#if 1
		//printf("Clock(): %g\n", MSG_get_clock());
		//printf("time: %g\n", time); 
		if(working && ceil(MSG_get_clock()) >= time){
			working = 0;
			random = (ran_distri(_group_info[client->group_number].nv_distri, _group_info[client->group_number].na_param, _group_info[client->group_number].nb_param)*3600.0);

			random = max(random, 0);
			if(ceil(random+MSG_get_clock()) > sim_duration){
				//printf("%f\n", random);
				random = max(sim_duration-MSG_get_clock(), 0);
				working = 1;
			}
			if(!working)
				grid_offline(client);
			FILE* unavailability = fopen("../exp/unavailability", "a+");
			fprintf(unavailability, "%0.1f\n", random / 3600);
			fclose(unavailability);
			
			notavailable += random;
			//printf("Lognormal: %f\n", random);

			if(client->running_project) {
                MSG_process_suspend(client->running_project->thread);
			}
	
			xbt_mutex_acquire(client->ask_for_work_mutex);
			client->suspended = random;
			xbt_cond_signal(client->work_fetch_cond);
			xbt_mutex_release(client->ask_for_work_mutex);

			
			//printf(" Cliente %s sleep %e\n", client->name, MSG_get_clock());
		
			MSG_process_sleep(random);
			
			if(client->running_project)			
        	        	MSG_process_resume(client->running_project->thread);  

			//printf(" Cliente %s RESUME %e\n", client->name, MSG_get_clock());           
			
		}
#endif

/*************** FIN SIMULAR CAIDA DEL CLIENTE ****/
		
		TRY {
			time_wait = min(sim_duration-MSG_get_clock(), _group_info[client->group_number].scheduling_interval);
			if(time_wait < 0) time_wait = 0;
			xbt_cond_timedwait(client->sched_cond, client->sched_mutex, time_wait);
		} CATCH (e) {time_sim++;xbt_ex_free(e);}
	}

	xbt_cond_signal(client->work_fetch_cond);

	xbt_mutex_acquire(client->ask_for_work_mutex);
        while (client->suspended != -1)
                xbt_cond_wait(client->ask_for_work_cond, client->ask_for_work_mutex);
        xbt_mutex_release(client->ask_for_work_mutex);

	//printf("Client %s finish at %e\n", client->name, MSG_get_clock());

// Imprimir resultados de ejecucion del cliente
#if 0
	xbt_dict_foreach(client->projects, cursor, key, proj) {
                printf("Client %s:   Projet: %s    total tasks executed: %d  total tasks received: %d total missed: %d\n",
                        client->name, proj->name, proj->total_tasks_executed,
                        proj->total_tasks_received, proj->total_tasks_missed);
        }
#endif

	// Print client finish
	//printf("Client %s %f GLOPS finish en %g sec. %g horas.\t Working: %0.1f%% \t Not working %0.1f%%\n", client->name, client->speed/1000000000.0, t0, t0/3600.0, available*100/(available+notavailable), (notavailable)*100/(available+notavailable));

	xbt_mutex_acquire(_group_info[client->group_number].mutex);
	_group_info[client->group_number].total_available += available*100/(available+notavailable);
	_group_info[client->group_number].total_notavailable += (notavailable)*100/(available+notavailable);
	_group_info[client->group_number].total_speed += speed;	
	xbt_mutex_release(_group_info[client->group_number].mutex);

	// Finish client
	xbt_mutex_acquire(_client_mutex);
	xbt_dict_foreach(client->projects, cursor, key, proj) {
		MSG_process_kill(proj->thread);
		_pdatabase[(int)proj->number].nfinished_clients++;
		//printf("%s, Num_clients: %d, Total_clients: %d\n", client->name, num_clients[proj->number], nclients[proj->number]);
		// Send finishing message to project_database
		if(_pdatabase[(int)proj->number].nfinished_clients == _pdatabase[(int)proj->number].nclients){	
			for(i=0; i<_pdatabase[(int)proj->number].nscheduling_servers; i++){
				msg = xbt_new0(s_ssmessage_t, 1);
				msg->type = TERMINATION;
				task = MSG_task_create("ask_addr", 0, 0, msg);
				MSG_task_send(task, _pdatabase[(int)proj->number].scheduling_servers[i]);
				task = NULL;	
			}
		}
	}
	xbt_mutex_release(_client_mutex);

	free_client(client);

	return 0;
}                               /* end_of_client */

/*****************************************************************************/

/** Test function */
msg_error_t test_all(const char *platform_file, const char *application_file)
{
	//printf("Executing test_all\n");
	msg_error_t res = MSG_OK;
	int i, days, hours, min;
	double t;			// Program time

	t = (double)time(NULL);	

	{       
		/*  Simulation setting */
		MSG_create_environment(platform_file);                          
		MSG_function_register("print_results", print_results);
		MSG_function_register("init_database", init_database);
		MSG_function_register("work_generator", work_generator);
		MSG_function_register("validator", validator);
		MSG_function_register("assimilator", assimilator);
		MSG_function_register("scheduling_server_requests", scheduling_server_requests);
		MSG_function_register("scheduling_server_dispatcher", scheduling_server_dispatcher);
		MSG_function_register("data_server_requests", data_server_requests);
		MSG_function_register("data_server_dispatcher", data_server_dispatcher);
		MSG_function_register("client", client);
		MSG_launch_application(application_file);
	}
		
	res = MSG_main();
	//printf( " Simulation time %g sec. %g horas\n", MSG_get_clock(), MSG_get_clock()/3600);

	for(i=0; i<NUMBER_CLIENT_GROUPS; i++){
		printf( " Group %d. Average speed: %f GFLOPS. Available: %0.1f%% Not available %0.1f%%\n", i, (double)_group_info[i].total_speed/_group_info[i].n_clients/1000000000.0, _group_info[i].total_available*100.0/(_group_info[i].total_available+_group_info[i].total_notavailable), (_group_info[i].total_notavailable)*100.0/(_group_info[i].total_available+_group_info[i].total_notavailable));
		_total_speed += _group_info[i].total_speed;
		_total_available += _group_info[i].total_available;
		_total_notavailable += _group_info[i].total_notavailable;
	}
	
	printf( "\n Clients. Average speed: %f GFLOPS. Available: %0.1f%% Not available %0.1f%%\n\n", (double)_total_speed/_num_clients_t/1000000000.0, _total_available*100.0/(_total_available+_total_notavailable), (_total_notavailable)*100.0/(_total_available+_total_notavailable));
	
	t = (double)time(NULL) - t;	// Program time
	days = (int)(t / (24*3600));	// Calculate days
	t -= (days*24*3600);
	hours = (int)(t/3600);		// Calculate hours
	t -= (hours*3600);
	min = (int)(t/60);		// Calculate minutes
	t -= (min*60);
	printf( " Execution time:\n %d days %d hours %d min %d s\n\n", days, hours, min, (int)round(t));

	return res;
}                               /* end_of_test_all */

/* Main function */
int main(int argc, char *argv[])
{
	int i, j;
	msg_error_t res;

	MSG_init(&argc, argv);
	
	if (argc != NUMBER_PROJECTS*2 + 4) {
		printf("Usage: %s PLATFORM_FILE DEPLOYMENT_FILE NUMBER_CLIENTS_PROJECT1 [NUMBER_CLIENTS_PROJECT2, ..., NUMBER_CLIENTS_PROJECTN] TOTAL_NUMBER_OF_CLIENTS \n", argv[0]);
		printf("Example: %s platform.xml deloyment.xml 1000 500 1200\n", argv[0]);
		exit(1);
	}

	seed(clock());

	remove("../exp/speed_statistics");
	remove("../exp/availability");
	remove("../exp/unavailability");
	remove("../exp/clients_dynamic");
	remove("../exp/sent_results");
	remove("../exp/got_results");
	remove("../exp/workunits_creation");
	remove("../exp/grid_utilization");

	_total_speed = 0;
	_total_available = 0;
	_total_notavailable = 0;
	_grid_online_power_deltas = xbt_new0(double, 10000000);
	_grid_idle_power_deltas = xbt_new0(double, 10000000);
	_pdatabase = xbt_new0(s_pdatabase_t, NUMBER_PROJECTS);
	_sserver_info = xbt_new0(s_sserver_t, NUMBER_SCHEDULING_SERVERS);
	_dserver_info = xbt_new0(s_dserver_t, NUMBER_DATA_SERVERS);
	_group_info = xbt_new0(s_group_t, NUMBER_CLIENT_GROUPS);

	for (i = 0; i < NUMBER_PROJECTS; i++) {
		
		/* Project attributes */

		_pdatabase[i].nclients = (int32_t) atoi(argv[i+3]);
		_pdatabase[i].nscheduling_servers = (char) atoi(argv[i+NUMBER_PROJECTS+3]);
		_pdatabase[i].scheduling_servers = xbt_new0(char*, (int) _pdatabase[i].nscheduling_servers);
		for(j=0; j<_pdatabase[i].nscheduling_servers; j++)
			_pdatabase[i].scheduling_servers[j] = bprintf("s%" PRId32 "%" PRId32, i+1, j);

		_pdatabase[i].nfinished_clients = 0;

		/* Work generator */

		_pdatabase[i].current_workunits = xbt_dict_new();		
		_pdatabase[i].ncurrent_results = 0;
		_pdatabase[i].current_results = xbt_queue_new(0, sizeof(result_t));		
		_pdatabase[i].r_mutex = xbt_mutex_init();
		_pdatabase[i].ncurrent_error_results = 0;
		_pdatabase[i].current_error_results = xbt_queue_new(0, sizeof(workunit_t));
		_pdatabase[i].er_mutex = xbt_mutex_init();
		_pdatabase[i].wg_empty = xbt_cond_init();
		_pdatabase[i].wg_full = xbt_cond_init();	
		_pdatabase[i].wg_err = xbt_cond_init();
		_pdatabase[i].wg_end = 0;

		/* Validator */
		
		_pdatabase[i].ncurrent_validations = 0;
		_pdatabase[i].current_validations = xbt_queue_new(0, sizeof(reply_t));		
		_pdatabase[i].v_mutex = xbt_mutex_init();
		_pdatabase[i].v_empty = xbt_cond_init();
		_pdatabase[i].v_end = 0;

		/* Assimilator */

		_pdatabase[i].ncurrent_assimilations = 0;
		_pdatabase[i].current_assimilations = xbt_queue_new(0, sizeof(char *));
		_pdatabase[i].a_mutex = xbt_mutex_init();
		_pdatabase[i].a_empty = xbt_cond_init();
		_pdatabase[i].a_end = 0;

		/* Input files */

		_pdatabase[i].ninput_files = 0;
		_pdatabase[i].input_files = xbt_queue_new(0, sizeof(int64_t));
		_pdatabase[i].i_mutex = xbt_mutex_init();
		_pdatabase[i].i_empty = xbt_cond_init();
		_pdatabase[i].i_full = xbt_cond_init();

		/* Output files */

		_pdatabase[i].noutput_files = 0;
		_pdatabase[i].output_files = xbt_queue_new(0, sizeof(int64_t));
		_pdatabase[i].o_mutex = xbt_mutex_init();
		_pdatabase[i].o_empty = xbt_cond_init();
		_pdatabase[i].o_full = xbt_cond_init();

		/* Synchronization */

		_pdatabase[i].ssrmutex = xbt_mutex_init();
		_pdatabase[i].ssdmutex = xbt_mutex_init();
		_pdatabase[i].barrier = MSG_barrier_init(_pdatabase[i].nscheduling_servers+4);
	}

	for (j = 0; j < NUMBER_SCHEDULING_SERVERS; j++){
		_sserver_info[j].mutex = xbt_mutex_init();
  		_sserver_info[j].cond = xbt_cond_init();
		_sserver_info[j].client_requests = xbt_queue_new(0, sizeof(ssmessage_t));
		_sserver_info[j].Nqueue = 0;
		_sserver_info[j].EmptyQueue = 0;
		_sserver_info[j].time_busy = 0;
	}

	for (j = 0; j < NUMBER_DATA_SERVERS; j++) {
		_dserver_info[j].mutex = xbt_mutex_init();
  		_dserver_info[j].cond = xbt_cond_init();
		_dserver_info[j].client_requests = xbt_queue_new(0, sizeof(dsmessage_t));
		_dserver_info[j].Nqueue = 0;
		_dserver_info[j].EmptyQueue = 0;
		_dserver_info[j].time_busy = 0;
	}

	for (j = 0; j < NUMBER_CLIENT_GROUPS; j++) {
		_group_info[j].total_speed = 0;
		_group_info[j].total_available = 0;
		_group_info[j].total_notavailable = 0;
		_group_info[j].tail_mean_speed = -1;
		_group_info[j].tail_availability = -1;
		_group_info[j].on = 0;
		_group_info[j].mutex = xbt_mutex_init();
		_group_info[j].cond = xbt_cond_init();
	}	

	_num_clients_t = atoi(argv[i*2+3]);
	_client_mutex = xbt_mutex_init();
	_grid_power_mutex = xbt_mutex_init();
	_sscomm = xbt_dict_new();
	_dscomm = xbt_dict_new();	

	res = test_all(argv[1], argv[2]);
	printf("tested\n");

	for (i = 0; i < NUMBER_PROJECTS; i++) {

		/* Project attributes */

		xbt_free(_pdatabase[i].project_name);
		for(j=0; j<_pdatabase[i].nscheduling_servers; j++)
			xbt_free(_pdatabase[i].scheduling_servers[j]);
		xbt_free(_pdatabase[i].scheduling_servers);

		/* Work results */

		xbt_dict_free(&_pdatabase[i].current_workunits);
		xbt_queue_free(&_pdatabase[i].current_results);
		xbt_mutex_destroy(_pdatabase[i].r_mutex);
		xbt_queue_free(&_pdatabase[i].current_error_results);
		xbt_mutex_destroy(_pdatabase[i].er_mutex);
		xbt_cond_destroy(_pdatabase[i].wg_empty);
		xbt_cond_destroy(_pdatabase[i].wg_full);
		xbt_cond_destroy(_pdatabase[i].wg_err);

		/* Validator */

		xbt_queue_free(&_pdatabase[i].current_validations);
		xbt_mutex_destroy(_pdatabase[i].v_mutex);
		xbt_cond_destroy(_pdatabase[i].v_empty);

		/* Assimilator */

		xbt_queue_free(&_pdatabase[i].current_assimilations);
		xbt_mutex_destroy(_pdatabase[i].a_mutex);
		xbt_cond_destroy(_pdatabase[i].a_empty);

		/* Input files */

		xbt_queue_free(&_pdatabase[i].input_files);
		xbt_mutex_destroy(_pdatabase[i].i_mutex);
		xbt_cond_destroy(_pdatabase[i].i_empty);
		xbt_cond_destroy(_pdatabase[i].i_full);

		/* Output files */

		xbt_queue_free(&_pdatabase[i].output_files);
		xbt_mutex_destroy(_pdatabase[i].o_mutex);
		xbt_cond_destroy(_pdatabase[i].o_empty);
		xbt_cond_destroy(_pdatabase[i].o_full);

		/* Synchronization */

		xbt_mutex_destroy(_pdatabase[i].ssrmutex);
		xbt_mutex_destroy(_pdatabase[i].ssdmutex);
		MSG_barrier_destroy(_pdatabase[i].barrier);
	}

	for (i = 0; i < NUMBER_SCHEDULING_SERVERS; i++){
		xbt_mutex_destroy(_sserver_info[i].mutex);
		xbt_cond_destroy(_sserver_info[i].cond);
		xbt_queue_free(&_sserver_info[i].client_requests);
	}

	for (i = 0; i < NUMBER_DATA_SERVERS; i++) {
		xbt_mutex_destroy(_dserver_info[i].mutex);
		xbt_cond_destroy(_dserver_info[i].cond);
		xbt_queue_free(&_dserver_info[i].client_requests);
	}

	for(i = 0; i < NUMBER_CLIENT_GROUPS; i++) {
		xbt_mutex_destroy(_group_info[i].mutex);
		xbt_cond_destroy(_group_info[i].cond);
	}		
		
	xbt_free(_pdatabase);
	xbt_free(_sserver_info);
	xbt_free(_dserver_info);
	xbt_free(_group_info);
	xbt_free(_grid_online_power_deltas);
	xbt_free(_grid_idle_power_deltas);
	xbt_mutex_destroy(_client_mutex);
	xbt_mutex_destroy(_grid_power_mutex);
	xbt_dict_free(&_sscomm);
	xbt_dict_free(&_dscomm);

	if (res == MSG_OK)
		return 0;
	else
		return 1;
}
